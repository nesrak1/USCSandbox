using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System.Runtime.InteropServices;

namespace USCSandbox.UltraShaderConverter.UShader.NVN
{
    public class NvnProgramToUSIL
    {
        private TranslatorContext _nvnShader;
        private DecodedProgram _prog;
        private Translator.FunctionCode[] _ryuIl;

        public UShaderProgram shader;

        private List<USILLocal> Locals => shader.locals;
        private List<USILInstruction> Instructions => shader.instructions;
        private List<USILInputOutput> Inputs => shader.inputs;
        private List<USILInputOutput> Outputs => shader.outputs;

        private delegate void InstHandler(Operation inst);
        private Dictionary<Instruction, InstHandler> _instructionHandlers;
        private Dictionary<Operand, int> _ryuLocals;

        private Dictionary<int, int> _resourceToDimension;

        public NvnProgramToUSIL(TranslatorContext nvnShader)
        {
            _nvnShader = nvnShader;
            _prog = nvnShader.Program;

            shader = new UShaderProgram();
            _instructionHandlers = new()
            {
                { Instruction.Copy, new InstHandler(HandleCopy) },
                { Instruction.Add, new InstHandler(HandleAdd) },
                { Instruction.Multiply | Instruction.FP32, new InstHandler(HandleMul) },
                { Instruction.Multiply | Instruction.FP64, new InstHandler(HandleMul) },
                //{ Instruction.Divide, new InstHandler(HandleDiv) },
                { Instruction.FusedMultiplyAdd | Instruction.FP32, new InstHandler(HandleMad) },
                { Instruction.FusedMultiplyAdd | Instruction.FP64, new InstHandler(HandleMad) },
                //{ Instruction.BitwiseAnd, new InstHandler(HandleAnd) },
                //{ Instruction.BitwiseOr, new InstHandler(HandleOr) },
                //{ Instruction.BitwiseExclusiveOr, new InstHandler(HandleXor) },
                //{ Instruction.BitwiseNot, new InstHandler(HandleNot) },
                //{ Instruction.ConvertFP32ToS32, new InstHandler(HandleFtoi) },
                //{ Instruction.ConvertFP32ToU32, new InstHandler(HandleFtoi) },
                //{ Instruction.ConvertS32ToFP32, new InstHandler(HandleItof) },
                //{ Instruction.ConvertU32ToFP32, new InstHandler(HandleItof) },
                //{ Instruction.Minimum, new InstHandler(HandleMin) },
                //{ Instruction.MinimumU32, new InstHandler(HandleMin) },
                //{ Instruction.Maximum, new InstHandler(HandleMax) },
                //{ Instruction.MaximumU32, new InstHandler(HandleMax) },
                //{ Instruction.SquareRoot, new InstHandler(HandleSqrt) },
                //{ Instruction.ReciprocalSquareRoot, new InstHandler(HandleRsq) },
                //{ Instruction.LogarithmB2, new InstHandler(HandleLog) },
                //{ Instruction.ExponentB2, new InstHandler(HandleExp) },
                //{ Instruction.Negate, new InstHandler(HandleNeg) },
                //{ Instruction.Round, new InstHandler(HandleRound) },
                //{ Instruction.Sine, new InstHandler(HandleSin) },
                //{ Instruction.Cosine, new InstHandler(HandleCos) },
                //{ Instruction.ShiftLeft, new InstHandler(HandleShl) },
                //{ Instruction.ShiftRightS32, new InstHandler(HandleIShr) },
                //{ Instruction.ShiftRightU32, new InstHandler(HandleIShr) },
                { Instruction.Load, new InstHandler(HandleLoad) },
                //{ Instruction.Store, new InstHandler(HandleStore) },

                //{ Instruction.dp2, new InstHandler(HandleDp2) },
                //{ Instruction.dp3, new InstHandler(HandleDp3) },
                //{ Instruction.dp4, new InstHandler(HandleDp4) },
                //{ Instruction.sample, new InstHandler(HandleSample) },
                //{ Instruction.sample_c, new InstHandler(HandleSampleC) },
                //{ Instruction.sample_c_lz, new InstHandler(HandleSampleC) },
                //{ Instruction.sample_l, new InstHandler(HandleSampleL) },
                //{ Instruction.sample_b, new InstHandler(HandleSampleL) },
                //{ Instruction.sample_d, new InstHandler(HandleSampleD) },
                //{ Instruction.ld, new InstHandler(HandleLd) },
                //{ Instruction.ldms, new InstHandler(HandleLd) },
                //{ Instruction.ld_structured, new InstHandler(HandleLdStructured) },
                //{ Instruction.discard, new InstHandler(HandleDiscard) },
                //{ Instruction.resinfo, new InstHandler(HandleResInfo) },
                //{ Instruction.sampleinfo, new InstHandler(HandleSampleInfo) },
                //{ Instruction.deriv_rtx, new InstHandler(HandleDerivRt) },
                //{ Instruction.deriv_rty, new InstHandler(HandleDerivRt) },
                //{ Instruction.deriv_rtx_coarse, new InstHandler(HandleDerivRt) },
                //{ Instruction.deriv_rty_coarse, new InstHandler(HandleDerivRt) },
                //{ Instruction.deriv_rtx_fine, new InstHandler(HandleDerivRt) },
                //{ Instruction.deriv_rty_fine, new InstHandler(HandleDerivRt) },
                //{ Instruction.@if, new InstHandler(HandleIf) },
                //{ Instruction.@else, new InstHandler(HandleElse) },
                //{ Instruction.endif, new InstHandler(HandleEndIf) },
                //{ Instruction.loop, new InstHandler(HandleLoop) },
                //{ Instruction.endloop, new InstHandler(HandleEndLoop) },
                //{ Instruction.@break, new InstHandler(HandleBreak) },
                //{ Instruction.breakc, new InstHandler(HandleBreakc) },
                //{ Instruction.@continue, new InstHandler(HandleContinue) },
                //{ Instruction.continuec, new InstHandler(HandleContinuec) },
                //{ Instruction.@switch, new InstHandler(HandleSwitch) },
                //{ Instruction.@case, new InstHandler(HandleCase) },
                //{ Instruction.@default, new InstHandler(HandleDefault) },
                //{ Instruction.endswitch, new InstHandler(HandleEndSwitch) },
                //{ Instruction.eq, new InstHandler(HandleEq) },
                //{ Instruction.ieq, new InstHandler(HandleEq) },
                //{ Instruction.ne, new InstHandler(HandleNeq) },
                //{ Instruction.ine, new InstHandler(HandleNeq) },
                //{ Instruction.lt, new InstHandler(HandleLt) },
                //{ Instruction.ilt, new InstHandler(HandleLt) },
                //{ Instruction.ult, new InstHandler(HandleLt) },
                //{ Instruction.ge, new InstHandler(HandleGe) },
                //{ Instruction.ige, new InstHandler(HandleGe) },
                //{ Instruction.uge, new InstHandler(HandleGe) },
                //{ Instruction.ret, new InstHandler(HandleRet) },
                //////dec
                //{ Opcode.dcl_temps, new InstHandler(HandleTemps) },
                //{ Opcode.dcl_resource, new InstHandler(HandleResource) },
                //{ Opcode.customdata, new InstHandler(HandleCustomData) }
            };

            // locals are not ID'd but pointers to specific operands
            // instead. we create a dictionary so we have actual IDs.
            _ryuLocals = new Dictionary<Operand, int>();
            _resourceToDimension = new Dictionary<int, int>();
        }

        public void Convert()
        {
            GenerateRyujinxIl();
            ConvertInstructions();
            var a = 2;
            //ConvertInputs();
            //ConvertOutputs();
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
                    Instructions.Add(new USILInstruction
                    {
                        instructionType = USILInstructionType.Comment,
                        destOperand = new USILOperand
                        {
                            comment = $"{disasm} // Unsupported",
                            operandType = USILOperandType.Comment
                        },
                        srcOperands = new List<USILOperand>()
                    });
                }
            }
        }

        private void FillUSILOperand(Operand mxOperand, USILOperand usilOperand, bool immIsInt)
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

                    usilOperand.operandType = USILOperandType.ConstantBuffer;
                    usilOperand.registerIndex = 3 - cbufSlot; // idk
                    usilOperand.arrayIndex = vecIndex;
                    usilOperand.mask = new int[] { new int[] { 0, 1, 2, 3 }[elemIndex] };
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
                        usilOperand.operandType = USILOperandType.TempRegister;
                        if (mxOperand.Type == OperandType.LocalVariable)
                        {
                            if (!_ryuLocals.ContainsKey(mxOperand))
                            {
                                _ryuLocals.Add(mxOperand, _ryuLocals.Count);
                            }
                            usilOperand.registerIndex = _ryuLocals[mxOperand] + 1000;
                        }
                        else
                        {
                            usilOperand.registerIndex = reg.Index;
                        }
                    }
                    else
                    {
                        // unsupported
                        //SetUsilOperandImmediate(usilOperand, 12345, 12345f, immIsInt);
                        usilOperand.operandType = USILOperandType.Comment;
                        usilOperand.comment = $"/*{mxOperand.Type}/{mxOperand.Value}/{reg.Type}/1*/";
                    }
                    break;
                }
                default:
                {
                    usilOperand.operandType = USILOperandType.Comment;
                    usilOperand.comment = $"/*{mxOperand.Type}/{mxOperand.Value}/2*/";
                    //SetUsilOperandImmediate(usilOperand, 23456, 23456f, immIsInt);
                    break;
                }
            }
        }

        private void SetUsilOperandImmediate(USILOperand usilOperand, int intValue, float floatValue, bool immIsInt)
        {
            usilOperand.operandType = immIsInt ? USILOperandType.ImmediateInt : USILOperandType.ImmediateFloat;
            if (immIsInt)
                usilOperand.immValueInt = new int[] { intValue };
            else
                usilOperand.immValueFloat = new float[] { floatValue };
        }

        private void HandleCopy(Operation inst)
        {
            Operand dest = inst.GetDest(0);
            Operand src0 = inst.GetSource(0);

            USILInstruction usilInst = new USILInstruction();
            USILOperand usilDest = new USILOperand();
            USILOperand usilSrc0 = new USILOperand();

            FillUSILOperand(dest, usilDest, false);
            FillUSILOperand(src0, usilSrc0, false);

            usilInst.instructionType = USILInstructionType.Move;
            usilInst.destOperand = usilDest;
            usilInst.srcOperands = new List<USILOperand>
            {
                usilSrc0
            };
            usilInst.saturate = false;

            Instructions.Add(usilInst);
        }

        private void HandleAdd(Operation inst)
        {
            Operand dest = inst.GetDest(0);
            Operand src0 = inst.GetSource(0);

            USILInstruction usilInst = new USILInstruction();
            USILOperand usilDest = new USILOperand();
            USILOperand usilSrc0 = new USILOperand();

            FillUSILOperand(dest, usilDest, false);
            FillUSILOperand(src0, usilSrc0, false);

            usilInst.instructionType = USILInstructionType.Add;
            usilInst.destOperand = usilDest;
            usilInst.srcOperands = new List<USILOperand>
            {
                usilSrc0
            };
            usilInst.saturate = false;

            Instructions.Add(usilInst);
        }

        private void HandleMul(Operation inst)
        {
            Operand dest = inst.GetDest(0);
            Operand src0 = inst.GetSource(0);
            Operand src1 = inst.GetSource(1);

            USILInstruction usilInst = new USILInstruction();
            USILOperand usilDest = new USILOperand();
            USILOperand usilSrc0 = new USILOperand();
            USILOperand usilSrc1 = new USILOperand();

            FillUSILOperand(dest, usilDest, false);
            FillUSILOperand(src0, usilSrc0, false);
            FillUSILOperand(src1, usilSrc1, false);

            usilInst.instructionType = USILInstructionType.Multiply;
            usilInst.destOperand = usilDest;
            usilInst.srcOperands = new List<USILOperand>
            {
                usilSrc0, usilSrc1
            };
            usilInst.saturate = false;

            Instructions.Add(usilInst);
        }

        private void HandleMad(Operation inst)
        {
            Operand dest = inst.GetDest(0);
            Operand src0 = inst.GetSource(0);
            Operand src1 = inst.GetSource(1);
            Operand src2 = inst.GetSource(2);

            USILInstruction usilInst = new USILInstruction();
            USILOperand usilDest = new USILOperand();
            USILOperand usilSrc0 = new USILOperand();
            USILOperand usilSrc1 = new USILOperand();
            USILOperand usilSrc2 = new USILOperand();

            FillUSILOperand(dest, usilDest, false);
            FillUSILOperand(src0, usilSrc0, false);
            FillUSILOperand(src1, usilSrc1, false);
            FillUSILOperand(src2, usilSrc2, false);

            usilInst.instructionType = USILInstructionType.MultiplyAdd;
            usilInst.destOperand = usilDest;
            usilInst.srcOperands = new List<USILOperand>
            {
                usilSrc0, usilSrc1, usilSrc2
            };
            usilInst.saturate = false;

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

                USILInstruction usilInst = new USILInstruction();
                USILOperand usilDest = new USILOperand();
                USILOperand usilSrc0 = new USILOperand();

                FillUSILOperand(dest, usilDest, false);
                //FillUSILOperand(src0, usilSrc0, false);
                //FillUSILOperand(src1, usilSrc1, false);
                usilSrc0.operandType = USILOperandType.Comment;
                usilSrc0.comment = $"/*{io}, {src1.Value}*/";

                usilInst.instructionType = USILInstructionType.Move;
                usilInst.destOperand = usilDest;
                usilInst.srcOperands = new List<USILOperand>
                {
                    usilSrc0
                };
                usilInst.saturate = false;

                Instructions.Add(usilInst);
            }
            else
            {
                string disasm = inst.Inst.ToString();
                Instructions.Add(new USILInstruction
                {
                    instructionType = USILInstructionType.Comment,
                    destOperand = new USILOperand
                    {
                        comment = $"{disasm} // Unsupported",
                        operandType = USILOperandType.Comment
                    },
                    srcOperands = new List<USILOperand>()
                });
            }
        }

        private void HandleStore(Operation inst)
        {
            Operand dest = inst.GetDest(0);
            Operand src0 = inst.GetSource(0);
            Operand src1 = inst.GetSource(1);

            USILInstruction usilInst = new USILInstruction();
            USILOperand usilDest = new USILOperand();
            USILOperand usilSrc0 = new USILOperand();
            USILOperand usilSrc1 = new USILOperand();

            FillUSILOperand(dest, usilDest, false);
            FillUSILOperand(src0, usilSrc0, false);
            FillUSILOperand(src1, usilSrc1, false);

            usilInst.instructionType = USILInstructionType.Multiply;
            usilInst.destOperand = usilDest;
            usilInst.srcOperands = new List<USILOperand>
            {
                usilSrc0, usilSrc1
            };
            usilInst.saturate = false;

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
}
