using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System.Runtime.InteropServices;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL;

namespace USCSandbox.ShaderCode.Converters.ToUsil;
public class NvnProgramToUsil
{
    private TranslatorContext _nvnShader;
    private DecodedProgram _prog;
    private Translator.FunctionCode[] _ryuIl;

    public UShaderProgram Shader;

    private List<UsilLocal> Locals => Shader.Locals;
    private List<UsilInstruction> Instructions => Shader.Instructions;
    private List<UsilInputOutput> Inputs => Shader.Inputs;
    private List<UsilInputOutput> Outputs => Shader.Outputs;

    private delegate void InstHandler(Operation inst);
    private Dictionary<Instruction, InstHandler> _instructionHandlers;
    private Dictionary<Operand, int> _ryuLocals;

    private Dictionary<int, int> _resourceToDimension;

    public NvnProgramToUsil(TranslatorContext nvnShader)
    {
        _nvnShader = nvnShader;
        _prog = nvnShader.Program;

        Shader = new UShaderProgram();
        _instructionHandlers = new()
        {
            { Instruction.Copy, new InstHandler(HandleCopy) },
            { Instruction.Add, new InstHandler(HandleAdd) },
            { Instruction.Multiply | Instruction.FP32, new InstHandler(HandleMul) },
            { Instruction.Multiply | Instruction.FP64, new InstHandler(HandleMul) },
            { Instruction.FusedMultiplyAdd | Instruction.FP32, new InstHandler(HandleMad) },
            { Instruction.FusedMultiplyAdd | Instruction.FP64, new InstHandler(HandleMad) },
            { Instruction.Load, new InstHandler(HandleLoad) },
        };

        // locals are not ID'd but pointers to specific operands
        // instead. we create a dictionary so we have actual IDs.
        _ryuLocals = [];
        _resourceToDimension = [];
        _ryuIl = [];
    }

    public void Convert()
    {
        GenerateRyujinxIl();
        ConvertInstructions();
    }

    private void GenerateRyujinxIl()
    {
        _ryuIl = Translator.EmitShader(_prog, _nvnShader.Config, true, out _);
    }

    private void ConvertInstructions()
    {
        Operation[] mainFuncCode = _ryuIl[0].Code;
        for (int i = 0; i < mainFuncCode.Length; i++)
        {
            Operation inst = mainFuncCode[i];
            if (_instructionHandlers.ContainsKey(inst.Inst))
            {
                _instructionHandlers[inst.Inst](inst);
            }
            else
            {
                string disasm = inst.Inst.ToString();
                Instructions.Add(new UsilInstruction
                {
                    InstructionType = UsilInstructionType.Comment,
                    DestOperand = new UsilOperand
                    {
                        Comment = $"{disasm} // Unsupported",
                        OperandType = UsilOperandType.Comment
                    },
                    SrcOperands = new List<UsilOperand>()
                });
            }
        }
    }

    private void FillUSILOperand(Operand mxOperand, UsilOperand usilOperand, bool immIsInt)
    {
        switch (mxOperand.Type)
        {
            case OperandType.Constant:
            {
                SetUsilOperandImmediate(usilOperand, mxOperand.Value, mxOperand.AsFloat(), immIsInt);
                break;
            }
            case OperandType.ConstantBuffer:
            {
                int cbufSlot = mxOperand.GetCbufSlot();
                int cbufOffset = mxOperand.GetCbufOffset();
                int vecIndex = cbufOffset >> 2;
                int elemIndex = cbufOffset & 3;

                usilOperand.OperandType = UsilOperandType.ConstantBuffer;
                usilOperand.RegisterIndex = 3 - cbufSlot; // idk
                usilOperand.ArrayIndex = vecIndex;
                usilOperand.Mask = [elemIndex];
                break;
            }
            case OperandType.Register:
            case OperandType.LocalVariable:
            {
                Register reg = mxOperand.GetRegister();

                if (reg.IsRZ)
                {
                    SetUsilOperandImmediate(usilOperand, 0, 0f, immIsInt);
                }
                else if (reg.Type == RegisterType.Gpr || reg.Type == RegisterType.Flag)
                {
                    usilOperand.OperandType = UsilOperandType.TempRegister;
                    if (mxOperand.Type == OperandType.LocalVariable)
                    {
                        if (!_ryuLocals.ContainsKey(mxOperand))
                        {
                            _ryuLocals.Add(mxOperand, _ryuLocals.Count);
                        }
                        usilOperand.RegisterIndex = _ryuLocals[mxOperand] + 1000;
                    }
                    else
                    {
                        usilOperand.RegisterIndex = reg.Index;
                    }
                }
                else
                {
                    // unsupported
                    usilOperand.OperandType = UsilOperandType.Comment;
                    usilOperand.Comment = $"/*{mxOperand.Type}/{mxOperand.Value}/{reg.Type}/1*/";
                }
                break;
            }
            default:
            {
                usilOperand.OperandType = UsilOperandType.Comment;
                usilOperand.Comment = $"/*{mxOperand.Type}/{mxOperand.Value}/2*/";
                break;
            }
        }
    }

    private void SetUsilOperandImmediate(UsilOperand usilOperand, int intValue, float floatValue, bool immIsInt)
    {
        usilOperand.OperandType = immIsInt ? UsilOperandType.ImmediateInt : UsilOperandType.ImmediateFloat;
        if (immIsInt)
            usilOperand.ImmValueInt = new int[] { intValue };
        else
            usilOperand.ImmValueFloat = new float[] { floatValue };
    }

    private void HandleCopy(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);

        usilInst.InstructionType = UsilInstructionType.Move;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleAdd(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);

        usilInst.InstructionType = UsilInstructionType.Add;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleMul(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();
        UsilOperand usilSrc1 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);
        FillUSILOperand(src1, usilSrc1, false);

        usilInst.InstructionType = UsilInstructionType.Multiply;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0, usilSrc1
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleMad(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);
        Operand src2 = inst.GetSource(2);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();
        UsilOperand usilSrc1 = new UsilOperand();
        UsilOperand usilSrc2 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);
        FillUSILOperand(src1, usilSrc1, false);
        FillUSILOperand(src2, usilSrc2, false);

        usilInst.InstructionType = UsilInstructionType.MultiplyAdd;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0, usilSrc1, usilSrc2
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleLoad(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);

        if (inst.StorageKind == StorageKind.Input)
        {
            IoVariable io = (IoVariable)src0.Value;

            UsilInstruction usilInst = new UsilInstruction();
            UsilOperand usilDest = new UsilOperand();
            UsilOperand usilSrc0 = new UsilOperand();

            FillUSILOperand(dest, usilDest, false);
            usilSrc0.OperandType = UsilOperandType.Comment;
            usilSrc0.Comment = $"/*{io}, {src1.Value}*/";

            usilInst.InstructionType = UsilInstructionType.Move;
            usilInst.DestOperand = usilDest;
            usilInst.SrcOperands = new List<UsilOperand>
            {
                usilSrc0
            };
            usilInst.Saturate = false;

            Instructions.Add(usilInst);
        }
        else
        {
            string disasm = inst.Inst.ToString();
            Instructions.Add(new UsilInstruction
            {
                InstructionType = UsilInstructionType.Comment,
                DestOperand = new UsilOperand
                {
                    Comment = $"{disasm} // Unsupported",
                    OperandType = UsilOperandType.Comment
                },
                SrcOperands = new List<UsilOperand>()
            });
        }
    }

    private void HandleStore(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();
        UsilOperand usilSrc1 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);
        FillUSILOperand(src1, usilSrc1, false);

        usilInst.InstructionType = UsilInstructionType.Multiply;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0, usilSrc1
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private class GpuAccessor : IGpuAccessor
    {
        private readonly byte[] _data;

        public GpuAccessor(byte[] data)
        {
            _data = data;
        }

        public ReadOnlySpan<ulong> GetCode(ulong address, int minimumSize)
        {
            return MemoryMarshal.Cast<byte, ulong>(new ReadOnlySpan<byte>(_data).Slice((int)address));
        }
    }
}
