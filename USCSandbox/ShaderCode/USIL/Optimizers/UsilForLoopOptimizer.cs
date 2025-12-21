using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Optimizers;
/// <summary>
/// Turns loops into for loop
/// </summary>
public class UsilForLoopOptimizer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        changes |= ReplaceForLoop(shader);

        return changes;
    }

    // Loop
    // BreakConditional (If / Break / EndIf)
    // ...
    // Add
    // EndLoop
    // ->
    // ForLoop
    // ...
    // EndLoop
    private bool ReplaceForLoop(UShaderProgram shader)
    {
        bool changes = false;
        Stack<LoopInstanceInfo> loopInfos = new Stack<LoopInstanceInfo>();

        int loopDepth = 0;

        List<UsilInstruction> insts = shader.Instructions;
        for (int i = 0; i < insts.Count - 5; i++)
        {
            // do detection

            if (insts[i].InstructionType == UsilInstructionType.EndLoop)
            {
                loopDepth--;
                if (loopInfos.Count > 0 && loopInfos.Peek().loopDepth == loopDepth)
                {
                    // didn't match and we're exiting this loop now
                    loopInfos.Pop();
                }
            }

            bool startOpcodesMatch =
                insts[i].InstructionType == UsilInstructionType.Loop &&
                IsComparisonInstruction(insts[i + 1]) &&
                IsIfInstruction(insts[i + 2]) &&
                insts[i + 3].InstructionType == UsilInstructionType.Break &&
                insts[i + 4].InstructionType == UsilInstructionType.EndIf;

            if (startOpcodesMatch)
            {
                UsilInstruction compInst = insts[i + 1];
                UsilInstruction ifInst = insts[i + 2];

                // todo doesn't check for iterator in other side of comparison
                bool breakcUsesComp =
                    compInst.DestOperand.RegisterIndex == ifInst.SrcOperands[0].RegisterIndex &&
                    DoMasksMatch(compInst.DestOperand.Mask, ifInst.SrcOperands[0].Mask);

                if (breakcUsesComp)
                {
                    UsilOperand iterRegOp = new UsilOperand(compInst.SrcOperands[0]);
                    UsilOperand compOp = new UsilOperand(compInst.SrcOperands[1]);
                    UsilInstructionType compType = compInst.InstructionType;
                    bool isInt = compInst.IsIntVariant;
                    bool isUnsigned = compInst.IsIntUnsigned;

                    LoopInstanceInfo loopInfo = new LoopInstanceInfo(iterRegOp, compOp, compType, isInt, isUnsigned, i, loopDepth);
                    loopInfos.Push(loopInfo);
                }
            }

            bool endOpcodesMatch =
                IsAddInstruction(insts[i]) &&
                insts[i + 1].InstructionType == UsilInstructionType.EndLoop;

            if (endOpcodesMatch)
            {
                if (loopInfos.Count > 0 && loopInfos.Peek().loopDepth == loopDepth - 1)
                {
                    LoopInstanceInfo loopInfo = loopInfos.Pop();

                    int startIndex = loopInfo.startIndex;
                    UsilInstruction forLoopInst = insts[startIndex];
                    UsilInstruction addIterInst = insts[i];

                    UsilNumberType numberType;
                    float addCount;

                    if (addIterInst.IsIntVariant)
                    {
                        if (addIterInst.IsIntUnsigned)
                        {
                            numberType = UsilNumberType.UnsignedInt;
                        }
                        else
                        {
                            numberType = UsilNumberType.Int;
                        }

                        addCount = addIterInst.SrcOperands[1].ImmInt[0];
                    }
                    else
                    {
                        numberType = UsilNumberType.Float;
                        addCount = addIterInst.SrcOperands[1].ImmFloat[0];
                    }

                    if (addIterInst.InstructionType == UsilInstructionType.Subtract)
                    {
                        addCount *= -1;
                    }

                    forLoopInst.InstructionType = UsilInstructionType.ForLoop;
                    forLoopInst.SrcOperands = new List<UsilOperand>
                    {
                        loopInfo.iterRegOp,
                        loopInfo.compOp,
                        new UsilOperand((int)InvertCompareType(loopInfo.compType)),
                        new UsilOperand((int)numberType),
                        new UsilOperand(addCount),
                        new UsilOperand(loopDepth - 1)
                    };

                    insts.RemoveAt(i); // Add/Subtract
                    insts.RemoveAt(startIndex + 1); // Compare
                    insts.RemoveAt(startIndex + 1); // If
                    insts.RemoveAt(startIndex + 1); // Break
                    insts.RemoveAt(startIndex + 1); // EndIf

                    i -= 4 - 1; // move iterator back for these five

                    changes = true;
                }
            }

            if (loopInfos.Count > 0)
            {
                List<UsilOperand> allOperands = GetAllOperands(insts[i]);
                foreach (UsilOperand op in allOperands)
                {
                    // todo: split mask from instruction if more than one component
                    if (op.OperandType == UsilOperandType.TempRegister && op.Mask.Length == 1)
                    {
                        foreach (LoopInstanceInfo loopInfo in loopInfos)
                        {
                            UsilOperand iterRegOp = loopInfo.iterRegOp;
                            bool matchesIter = op.RegisterIndex == iterRegOp.RegisterIndex &&
                                op.Mask[0] == iterRegOp.Mask[0];

                            if (!matchesIter)
                            {
                                break;
                            }

                            op.MetadataName = UsilConstants.ITER_CHARS[loopInfo.loopDepth].ToString();
                            op.MetadataNameAssigned = true;

                            op.DisplayMask = false;
                        }
                    }
                }
            }

            if (insts[i].InstructionType == UsilInstructionType.Loop)
            {
                loopDepth++;
            }
        }
        return changes;
    }

    private bool IsComparisonInstruction(UsilInstruction instruction)
    {
        switch (instruction.InstructionType)
        {
            case UsilInstructionType.Equal:
            case UsilInstructionType.NotEqual:
            case UsilInstructionType.GreaterThan:
            case UsilInstructionType.GreaterThanOrEqual:
            case UsilInstructionType.LessThan:
            case UsilInstructionType.LessThanOrEqual:
                return true;
            default:
                return false;
        }
    }

    private bool IsIfInstruction(UsilInstruction instruction)
    {
        switch (instruction.InstructionType)
        {
            case UsilInstructionType.IfTrue:
            case UsilInstructionType.IfFalse:
                return true;
            default:
                return false;
        }
    }

    private bool IsAddInstruction(UsilInstruction instruction)
    {
        switch (instruction.InstructionType)
        {
            case UsilInstructionType.Add:
            case UsilInstructionType.Subtract:
                return true;
            default:
                return false;
        }
    }

    private UsilInstructionType InvertCompareType(UsilInstructionType type)
    {
        switch (type)
        {
            case UsilInstructionType.LessThan:
                return UsilInstructionType.GreaterThanOrEqual;
            case UsilInstructionType.GreaterThan:
                return UsilInstructionType.LessThanOrEqual;
            case UsilInstructionType.LessThanOrEqual:
                return UsilInstructionType.GreaterThan;
            case UsilInstructionType.GreaterThanOrEqual:
                return UsilInstructionType.LessThan;
            default:
                return type;
        }
    }

    private bool DoOpcodesMatch(List<UsilInstruction> insts, int startIndex, UsilInstructionType[] instTypes)
    {
        if (startIndex + instTypes.Length > insts.Count)
        {
            return false;
        }

        for (int i = 0; i < instTypes.Length; i++)
        {
            if (insts[startIndex + i].InstructionType != instTypes[i])
            {
                return false;
            }
        }
        return true;
    }

    private bool DoMasksMatch(int[] maskA, int[] maskB)
    {
        if (maskA.Length != maskB.Length)
        {
            return false;
        }

        for (int i = 0; i < maskB.Length; i++)
        {
            if (maskA[i] != maskB[i])
            {
                return false;
            }
        }
        return true;
    }

    private List<UsilOperand> GetAllOperands(UsilInstruction inst)
    {
        List<UsilOperand> operands = new List<UsilOperand>();

        if (inst.DestOperand != null)
        {
            operands.AddRange(GetAllOperands(inst.DestOperand));
        }

        if (inst.SrcOperands != null)
        {
            foreach (UsilOperand srcOp in inst.SrcOperands)
            {
                operands.AddRange(GetAllOperands(srcOp));
            }
        }

        return operands;
    }

    private List<UsilOperand> GetAllOperands(UsilOperand operand)
    {
        if (operand.ArrayRelative == null && (operand.Children == null || operand.Children.Length == 0))
        {
            return new List<UsilOperand> { operand };
        }

        List<UsilOperand> operands = new List<UsilOperand>()
        {
            operand
        };

        if (operand.ArrayRelative != null)
        {
            operands.AddRange(GetAllOperands(operand.ArrayRelative));
        }

        if (operand.Children != null)
        {
            foreach (UsilOperand child in operand.Children)
            {
                operands.AddRange(GetAllOperands(child));
            }
        }

        return operands;
    }

    class LoopInstanceInfo
    {
        public UsilOperand iterRegOp;
        public UsilOperand compOp;
        public UsilInstructionType compType;
        public bool isInt;
        public bool isUnsigned;
        public int startIndex;
        public int loopDepth;

        public LoopInstanceInfo(
            UsilOperand iterRegOp, UsilOperand compOp, UsilInstructionType compType,
            bool isInt, bool isUnsigned, int startIndex, int loopDepth)
        {
            this.iterRegOp = iterRegOp;
            this.compOp = compOp;
            this.compType = compType;
            this.isInt = isInt;
            this.isUnsigned = isUnsigned;
            this.startIndex = startIndex;
            this.loopDepth = loopDepth;
        }
    }
}
