using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL;
using RyuOperandType = Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandType;

namespace USCSandbox.ShaderCode.Converters.ToUsil;
public class NvnProgramToUsil
{
    private TranslatorContext _nvnShader;
    private DecodedProgram _prog;
    private INode[] _ryuIl;

    public UShaderProgram Shader;

    private List<UsilLocal> Locals => Shader.Locals;
    private List<UsilInstruction> Instructions => Shader.Instructions;
    private List<UsilInputOutput> Inputs => Shader.Inputs;
    private List<UsilInputOutput> Outputs => Shader.Outputs;

    private delegate void InstHandler(Operation inst);
    private Dictionary<Instruction, InstHandler> _instructionHandlers;
    private Dictionary<Operand, int> _ryuLabels;
    private Dictionary<Operand, int> _ryuLocals;
    private Dictionary<BasicBlock, int> _blockIdxMap;

    private Dictionary<int, int> _resourceToDimension;

    public NvnProgramToUsil(TranslatorContext nvnShader)
    {
        _nvnShader = nvnShader;
        _prog = nvnShader.Program;

        Shader = new UShaderProgram();
        _instructionHandlers = new()
        {
            { Instruction.Add, new InstHandler(HandleAdd) },
            { Instruction.Clamp, new InstHandler(HandleClamp) },
            { Instruction.Comment, new InstHandler(HandleComment) },
            { Instruction.Copy, new InstHandler(HandleCopy) },
            { Instruction.Divide, new InstHandler(HandleDivide) },
            { Instruction.FusedMultiplyAdd, new InstHandler(HandleMad) },
            { Instruction.Load, new InstHandler(HandleLoadStore) },
            { Instruction.Maximum, new InstHandler(HandleMaximum) },
            { Instruction.MaximumU32, new InstHandler(HandleMaximum) },
            { Instruction.Minimum, new InstHandler(HandleMinimum) },
            { Instruction.MinimumU32, new InstHandler(HandleMinimum) },
            { Instruction.Multiply, new InstHandler(HandleMul) },
            { Instruction.Negate, new InstHandler(HandleNegate) },
            { Instruction.SquareRoot, new InstHandler(HandleSquareRoot) },
            { Instruction.Store, new InstHandler(HandleLoadStore) },
            { Instruction.Subtract, new InstHandler(HandleAdd) },
            { Instruction.ReciprocalSquareRoot, new InstHandler(HandleRSquareRoot) },
            { Instruction.Return, new InstHandler(HandleReturn) }
        };

        // locals are not ID'd but pointers to specific operands
        // instead. we create a dictionary so we have actual IDs.
        _ryuLocals = [];
        _ryuLabels = [];
        _resourceToDimension = [];
        _ryuIl = [];
        _blockIdxMap = [];
    }

    public void Convert()
    {
        GenerateRyujinxIl();
        ConvertInstructions();
    }

    private void GenerateRyujinxIl()
    {
        //ResourceManager resourceManager = _nvnShader.CreateResourceManager(false);
        //_ryuIl = Translator.EmitShader(_nvnShader, resourceManager, _prog, false, true, out _);
        var funcs = _nvnShader.TranslateToFunctions();
        var func = funcs[0];
        var insts = new List<INode>();

        var blockIdx = 0;
        foreach (var block in func.Blocks)
        {
            _blockIdxMap[block] = blockIdx++;
        }

        foreach (var block in func.Blocks)
        {
            insts.Add(new CommentNode($"Block {_blockIdxMap[block]}"));
            insts.AddRange(block.Operations);
            if (block.HasBranch)
            {
                insts.Add(new CommentNode($"  Block {_blockIdxMap[block]} BT -> Block {_blockIdxMap[block.Branch]}"));
                if (block.Next != null)
                    insts.Add(new CommentNode($"  Block {_blockIdxMap[block]} BF -> Block {_blockIdxMap[block.Next]}"));
            }
            else
            {
                if (block.Next != null)
                    insts.Add(new CommentNode($"  Block {_blockIdxMap[block]} BU -> Block {_blockIdxMap[block.Next]}"));
            }
        }
        _ryuIl = insts.ToArray();
    }

    private void ConvertInstructions()
    {
        //Operation[] mainFuncCode = _ryuIl[0].Code;

        Console.WriteLine(">>>");
        Console.WriteLine(_nvnShader.Translate().Code);
        Console.WriteLine("---");
        foreach (INode node in _ryuIl)
        {
            if (node is Operation operation)
            {
                Console.WriteLine(RyuOperationToString(operation));
            }
            else if (node is PhiNode phiNode)
            {
                Console.WriteLine(RyuPhiNodeToString(phiNode));
            }
            else
            {
                Console.WriteLine("???");
            }
        }
        Console.WriteLine("<<<");

        foreach (INode node in _ryuIl)
        {
            string disasm = "???";
            if (node is Operation inst)
            {
                disasm = RyuOperationToString(inst);
                Instruction maskedInst = inst.Inst & Instruction.Mask;
                if (_instructionHandlers.ContainsKey(maskedInst))
                {
                    _instructionHandlers[maskedInst](inst);
                    continue;
                }
            }
            else if (node is PhiNode phiNode)
            {
                disasm = RyuPhiNodeToString(phiNode);
            }

            Instructions.Add(new UsilInstruction
            {
                InstructionType = UsilInstructionType.Comment,
                DestOperand = new UsilOperand
                {
                    Comment = $"{disasm}",
                    OperandType = UsilOperandType.Comment
                },
                SrcOperands = new List<UsilOperand>()
            });
        }
    }

    // move
    private string RyuOperationToString(Operation operation)
    {
        string maskedInst = (operation.Inst & Instruction.Mask).ToString();
        if (operation.Inst.HasFlag(Instruction.FP32))
            maskedInst += ".FP32";
        if (operation.Inst.HasFlag(Instruction.FP64))
            maskedInst += ".FP64";

        string storeKind = operation.StorageKind.ToString();

        List<string> destStrs = Enumerable.Range(0, operation.DestsCount)
            .Select(i => RyuOperandToString(operation.GetDest(i)))
            .ToList();

        List<string> srcStrs = Enumerable.Range(0, operation.SourcesCount)
            .Select(i => RyuOperandToString(operation.GetSource(i)))
            .ToList();

        return $"{maskedInst}({storeKind}) {string.Join(",", destStrs)} <= {string.Join(",", srcStrs)}";
    }

    // move
    private string RyuPhiNodeToString(PhiNode operation)
    {
        List<string> destStrs = Enumerable.Range(0, operation.DestsCount)
            .Select(i => RyuOperandToString(operation.GetDest(i)))
            .ToList();

        List<string> srcStrs = Enumerable.Range(0, operation.SourcesCount)
            .Select(i => $"{RyuOperandToString(operation.GetSource(i))}:Block{_blockIdxMap[operation.GetBlock(i)]}")
            .ToList();

        return $"$phi {string.Join(",", destStrs)} <= {string.Join(",", srcStrs)}";
    }

    // move
    private string RyuOperandToString(Operand operand)
    {
        switch (operand.Type)
        {
            case RyuOperandType.Argument:
                return $"[arg:{operand.Value}]";
            case RyuOperandType.Constant:
                return $"[con:{operand.Value}]";
            case RyuOperandType.ConstantBuffer:
                return $"[cbf:{operand.GetCbufSlot()}:{operand.GetCbufOffset()}]";
            case RyuOperandType.Label:
                if (!_ryuLabels.ContainsKey(operand))
                {
                    _ryuLabels.Add(operand, _ryuLabels.Count);
                }
                return $"[lbl:{_ryuLabels[operand]}]";
            case RyuOperandType.LocalVariable:
                if (!_ryuLocals.ContainsKey(operand))
                {
                    _ryuLocals.Add(operand, _ryuLocals.Count);
                }
                return $"[var:L{_ryuLocals[operand]}]";
            case RyuOperandType.Register:
                return $"[reg:{RyuRegisterToString(operand.GetRegister())}]";
            case RyuOperandType.Undefined:
                return "[undef]";
            default:
                return "[unk]";
        }
    }

    // move
    private string RyuRegisterToString(Register register)
    {
        string typePrefix = register.Type switch
        {
            RegisterType.Flag => "F",
            RegisterType.Gpr => "R",
            RegisterType.Predicate => "P",
            _ => "?"
        };

        return $"{typePrefix}{register.Index}{(register.IsPT ? "P" : "")}{(register.IsRZ ? "R" : "")}";
    }

    private void FillUSILOperand(Operand mxOperand, UsilOperand usilOperand, bool immIsInt)
    {
        switch (mxOperand.Type)
        {
            case RyuOperandType.Constant:
            {
                SetUsilOperandImmediate(usilOperand, mxOperand.Value, mxOperand.AsFloat(), immIsInt);
                break;
            }
            case RyuOperandType.ConstantBuffer:
            {
                // ??? probably needs fixing
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
            case RyuOperandType.Register:
            case RyuOperandType.LocalVariable:
            {
                Register reg = mxOperand.GetRegister();

                if (reg.IsRZ)
                {
                    SetUsilOperandImmediate(usilOperand, 0, 0f, immIsInt);
                }
                else if (reg.Type == RegisterType.Gpr || reg.Type == RegisterType.Flag)
                {
                    usilOperand.OperandType = UsilOperandType.TempRegister;
                    if (mxOperand.Type == RyuOperandType.LocalVariable)
                    {
                        if (!_ryuLocals.ContainsKey(mxOperand))
                        {
                            _ryuLocals.Add(mxOperand, _ryuLocals.Count);
                        }
                        usilOperand.RegisterIndex = _ryuLocals[mxOperand];
                    }
                    else
                    {
                        usilOperand.RegisterIndex = reg.Index + 1000;
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
            usilOperand.ImmInt = new int[] { intValue };
        else
            usilOperand.ImmFloat = new float[] { floatValue };
    }

    private void HandleAdd(Operation inst)
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

        if (inst.Inst == Instruction.Add)
        {
            usilInst.InstructionType = UsilInstructionType.Add;
        }
        else
        {
            usilInst.InstructionType = UsilInstructionType.Subtract;
        }

        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0,
            usilSrc1
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleClamp(Operation inst)
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

        bool isInt = inst.Inst == Instruction.ClampU32;

        FillUSILOperand(dest, usilDest, isInt);
        FillUSILOperand(src0, usilSrc0, isInt);
        FillUSILOperand(src1, usilSrc1, isInt);
        FillUSILOperand(src2, usilSrc2, isInt);

        if (isInt)
        {
            usilInst.InstructionType = UsilInstructionType.ClampUInt;
        }
        else
        {
            usilInst.InstructionType = UsilInstructionType.Clamp;
        }

        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0,
            usilSrc1,
            usilSrc2
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleComment(Operation inst)
    {
        var commentInst = (CommentNode)inst;
        Instructions.Add(new UsilInstruction
        {
            InstructionType = UsilInstructionType.Comment,
            DestOperand = new UsilOperand
            {
                Comment = commentInst.Comment,
                OperandType = UsilOperandType.Comment
            },
            SrcOperands = new List<UsilOperand>()
        });
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

    private void HandleDivide(Operation inst)
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

        usilInst.InstructionType = UsilInstructionType.Divide;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0, usilSrc1
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleLoadStore(Operation inst)
    {
        bool isStore = inst.Inst == Instruction.Store;

        string debug = RyuOperationToString(inst);

        int srcIdx = 0;
        StorageKind storKind = inst.StorageKind;

        if (storKind.IsInputOrOutput())
        {
            Operand? dest = !isStore ? inst.GetDest(0) : null;

            Operand srcVarId = inst.GetSource(srcIdx++);
            IoVariable io = (IoVariable)srcVarId.Value;

            UsilInstruction usilInst = new UsilInstruction();
            UsilOperand usilDest = new UsilOperand();
            UsilOperand usilSrc0 = new UsilOperand();

            UsilOperand specialOp = !isStore ? usilSrc0 : usilDest;
            switch (io)
            {
                case IoVariable.UserDefined:
                case IoVariable.FragmentOutputColor:
                {
                    Operand srcRegIndex = inst.GetSource(srcIdx++);
                    Operand srcMaskIndex = inst.GetSource(srcIdx++);

                    specialOp.OperandType = storKind switch
                    {
                        StorageKind.Input or
                        StorageKind.InputPerPatch => UsilOperandType.InputRegister,
                        StorageKind.Output or
                        StorageKind.OutputPerPatch => UsilOperandType.OutputRegister,
                        _ => throw new Exception("invalid storage kind")
                    };
                    specialOp.RegisterIndex = srcRegIndex.Value;
                    specialOp.Mask = [srcMaskIndex.Value];
                    break;
                }
                default:
                {
                    goto unsupported;
                }
            }

            if (!isStore && dest != null)
            {
                FillUSILOperand(dest, usilDest, false);
            }
            else
            {
                Operand storeVal = inst.GetSource(srcIdx++);
                FillUSILOperand(storeVal, usilSrc0, false);
            }

            usilInst.InstructionType = UsilInstructionType.Move;
            usilInst.DestOperand = usilDest;
            usilInst.SrcOperands = new List<UsilOperand>
            {
                usilSrc0
            };
            usilInst.Saturate = false;

            Instructions.Add(usilInst);
            return;
        }

    unsupported:
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

    private void HandleMaximum(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();
        UsilOperand usilSrc1 = new UsilOperand();

        bool isInt = inst.Inst == Instruction.MaximumU32;

        FillUSILOperand(dest, usilDest, isInt);
        FillUSILOperand(src0, usilSrc0, isInt);
        FillUSILOperand(src1, usilSrc1, isInt);

        usilInst.InstructionType = UsilInstructionType.Maximum;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0,
            usilSrc1,
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleMinimum(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);
        Operand src1 = inst.GetSource(1);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();
        UsilOperand usilSrc1 = new UsilOperand();

        bool isInt = inst.Inst == Instruction.MinimumU32;

        FillUSILOperand(dest, usilDest, isInt);
        FillUSILOperand(src0, usilSrc0, isInt);
        FillUSILOperand(src1, usilSrc1, isInt);

        usilInst.InstructionType = UsilInstructionType.Minimum;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0,
            usilSrc1,
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

    private void HandleNegate(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);

        usilInst.InstructionType = UsilInstructionType.Negate;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleSquareRoot(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);

        usilInst.InstructionType = UsilInstructionType.SquareRoot;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleRSquareRoot(Operation inst)
    {
        Operand dest = inst.GetDest(0);
        Operand src0 = inst.GetSource(0);

        UsilInstruction usilInst = new UsilInstruction();
        UsilOperand usilDest = new UsilOperand();
        UsilOperand usilSrc0 = new UsilOperand();

        FillUSILOperand(dest, usilDest, false);
        FillUSILOperand(src0, usilSrc0, false);

        usilInst.InstructionType = UsilInstructionType.SquareRootReciprocal;
        usilInst.DestOperand = usilDest;
        usilInst.SrcOperands = new List<UsilOperand>
        {
            usilSrc0
        };
        usilInst.Saturate = false;

        Instructions.Add(usilInst);
    }

    private void HandleReturn(Operation inst)
    {
        UsilInstruction usilInst = new UsilInstruction();
        usilInst.InstructionType = UsilInstructionType.Return;
        usilInst.SrcOperands = new List<UsilOperand>();
        Instructions.Add(usilInst);
    }
}
