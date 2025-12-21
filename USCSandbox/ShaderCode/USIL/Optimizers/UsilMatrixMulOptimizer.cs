using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;
using static USCSandbox.ShaderCode.USIL.UsilOptimizerUtil;

namespace USCSandbox.ShaderCode.USIL.Optimizers;
/// <summary>
/// Converts multiple multiply operations into a single matrix one
/// "instruction"
/// </summary>
/// <remarks>
/// Note: cbuffers must be converted to matrix type by this point.
/// It's a miracle when this works. There's so many issues with how this works fundamentally.
/// </remarks>
public class UsilMatrixMulOptimizer : IUsilOptimizer
{
    private static readonly int[] XYZW_MASK = new int[] { 0, 1, 2, 3 };
    private static readonly int[] XXXX_MASK = new int[] { 0, 0, 0, 0 };
    private static readonly int[] YYYY_MASK = new int[] { 1, 1, 1, 1 };
    private static readonly int[] ZZZZ_MASK = new int[] { 2, 2, 2, 2 };
    private static readonly int[] WWWW_MASK = new int[] { 3, 3, 3, 3 };

    private static readonly int[] XYZ_MASK = new int[] { 0, 1, 2 };
    private static readonly int[] XXX_MASK = new int[] { 0, 0, 0 };
    private static readonly int[] YYY_MASK = new int[] { 1, 1, 1 };
    private static readonly int[] ZZZ_MASK = new int[] { 2, 2, 2 };

    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        changes |= ReplaceMulMatrixVec4W1(shader);
        changes |= ReplaceMulMatrixVec4(shader);
        changes |= ReplaceMulMatrixVec3(shader);

        return changes;
    }

    // mat4x4 * vec4(vec3, 1)
    private static bool ReplaceMulMatrixVec4W1(UShaderProgram shader)
    {
        bool changes = false;

        List<UsilInstruction> insts = shader.Instructions;
        for (int i = 0; i < insts.Count - 3; i++)
        {
            // do detection

            bool opcodesMatch = DoOpcodesMatch(insts, i, new[] {
                UsilInstructionType.Multiply,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.Add
            });

            if (!opcodesMatch)
            {
                continue;
            }

            UsilInstruction inst0 = insts[i];
            UsilInstruction inst1 = insts[i + 1];
            UsilInstruction inst2 = insts[i + 2];
            UsilInstruction inst3 = insts[i + 3];

            bool matricesCorrect =
                inst0.SrcOperands[1].OperandType == UsilOperandType.Matrix &&
                inst0.SrcOperands[1].ArrayIndex == 1 &&
                DoMasksMatch(inst0.SrcOperands[1], XYZW_MASK) &&

                inst1.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst1.SrcOperands[0].ArrayIndex == 0 &&
                DoMasksMatch(inst1.SrcOperands[0], XYZW_MASK) &&

                inst2.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst2.SrcOperands[0].ArrayIndex == 2 &&
                DoMasksMatch(inst2.SrcOperands[0], XYZW_MASK) &&

                inst3.SrcOperands[1].OperandType == UsilOperandType.Matrix &&
                inst3.SrcOperands[1].ArrayIndex == 3 &&
                DoMasksMatch(inst3.SrcOperands[1], XYZW_MASK);

            if (!matricesCorrect)
            {
                continue;
            }

            int tmp0Index = inst0.DestOperand.RegisterIndex;
            int tmp1Index = inst1.DestOperand.RegisterIndex;
            int tmp2Index = inst2.DestOperand.RegisterIndex;
            int tmp3Index = inst3.DestOperand.RegisterIndex;

            // registers can swap halfway through to be used for something else
            // don't try to convert the matrix because we can't handle this yet
            if (tmp0Index != tmp1Index || tmp1Index != tmp2Index || tmp2Index != tmp3Index)
            {
                continue;
            }

            bool tempRegisterCorrect =
                inst0.DestOperand.RegisterIndex == tmp0Index &&
                inst1.DestOperand.RegisterIndex == tmp0Index &&
                inst1.SrcOperands[2].RegisterIndex == tmp0Index &&
                inst2.SrcOperands[2].RegisterIndex == tmp0Index &&

                inst2.DestOperand.RegisterIndex == tmp1Index &&
                inst3.SrcOperands[0].RegisterIndex == tmp1Index;

            if (!tempRegisterCorrect)
            {
                continue;
            }

            // todo: input isn't guaranteed temp
            // todo: is input guaranteed to start at x?
            int inpIndex = inst0.SrcOperands[0].RegisterIndex;
            bool inputsCorrect =
                inst0.SrcOperands[0].RegisterIndex == inpIndex &&
                DoMasksMatch(inst0.SrcOperands[0], YYYY_MASK) &&

                inst1.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst1.SrcOperands[1], XXXX_MASK) &&

                inst2.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst2.SrcOperands[1], ZZZZ_MASK);

            if (!inputsCorrect)
            {
                continue;
            }

            // make replacement

            UsilOperand mulInputVec3Operand = new UsilOperand(inst0.SrcOperands[0]);
            UsilOperand mulInputMat4x4Operand = new UsilOperand(inst0.SrcOperands[1]);
            UsilOperand mulOutputOperand = new UsilOperand(inst3.DestOperand);

            mulInputMat4x4Operand.DisplayMask = false;
            mulInputVec3Operand.Mask = new int[] { 0, 1, 2 };

            UsilOperand mulInput1Operand = new UsilOperand()
            {
                OperandType = UsilOperandType.ImmediateFloat,
                ImmFloat = new[] { 1f },
            };

            UsilOperand mulInputVec4Operand = new UsilOperand()
            {
                OperandType = UsilOperandType.Multiple,
                Children = new[] { mulInputVec3Operand, mulInput1Operand }
            };

            UsilInstruction mulInstruction = new UsilInstruction()
            {
                InstructionType = UsilInstructionType.MultiplyMatrixByVector,
                DestOperand = mulOutputOperand,
                SrcOperands = new List<UsilOperand> { mulInputMat4x4Operand, mulInputVec4Operand }
            };

            insts.RemoveRange(i, 4);
            insts.Insert(i, mulInstruction);

            changes = true;
        }
        return changes;
    }

    // mat4x4 * vec4
    private static bool ReplaceMulMatrixVec4(UShaderProgram shader)
    {
        bool changes = false;

        List<UsilInstruction> insts = shader.Instructions;
        for (int i = 0; i < insts.Count - 3; i++)
        {
            // do detection

            bool opcodesMatch = DoOpcodesMatch(insts, i, new[] {
                UsilInstructionType.Multiply,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.MultiplyAdd
            });

            if (!opcodesMatch)
            {
                continue;
            }

            UsilInstruction inst0 = insts[i];
            UsilInstruction inst1 = insts[i + 1];
            UsilInstruction inst2 = insts[i + 2];
            UsilInstruction inst3 = insts[i + 3];

            bool matricesCorrect =
                inst0.SrcOperands[1].OperandType == UsilOperandType.Matrix &&
                inst0.SrcOperands[1].ArrayIndex == 1 &&
                DoMasksMatch(inst0.SrcOperands[1], XYZW_MASK) &&

                inst1.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst1.SrcOperands[0].ArrayIndex == 0 &&
                DoMasksMatch(inst1.SrcOperands[0], XYZW_MASK) &&

                inst2.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst2.SrcOperands[0].ArrayIndex == 2 &&
                DoMasksMatch(inst2.SrcOperands[0], XYZW_MASK) &&

                inst3.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst3.SrcOperands[0].ArrayIndex == 3 &&
                DoMasksMatch(inst3.SrcOperands[0], XYZW_MASK);

            if (!matricesCorrect)
            {
                continue;
            }

            int tmp0Index = inst0.DestOperand.RegisterIndex;
            int tmp1Index = inst1.DestOperand.RegisterIndex;
            int tmp2Index = inst2.DestOperand.RegisterIndex;
            int tmp3Index = inst3.DestOperand.RegisterIndex;

            // registers can swap halfway through to be used for something else
            // don't try to convert the matrix because we can't handle this yet
            if (tmp0Index != tmp1Index || tmp1Index != tmp2Index || tmp2Index != tmp3Index)
            {
                continue;
            }

            int tmpIndex = inst0.DestOperand.RegisterIndex;
            bool tempRegisterCorrect =
                inst0.DestOperand.RegisterIndex == tmpIndex &&

                inst1.DestOperand.RegisterIndex == tmpIndex &&
                inst1.SrcOperands[2].RegisterIndex == tmpIndex &&

                inst2.DestOperand.RegisterIndex == tmpIndex &&
                inst2.SrcOperands[2].RegisterIndex == tmpIndex &&

                inst3.SrcOperands[2].RegisterIndex == tmpIndex;

            if (!tempRegisterCorrect)
            {
                continue;
            }

            // todo: input isn't guaranteed temp
            int inpIndex = inst0.SrcOperands[0].RegisterIndex;
            bool inputsCorrect =
                inst0.SrcOperands[0].RegisterIndex == inpIndex &&
                DoMasksMatch(inst0.SrcOperands[0], YYYY_MASK) &&

                inst1.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst1.SrcOperands[1], XXXX_MASK) &&

                inst2.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst2.SrcOperands[1], ZZZZ_MASK) &&

                inst3.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst3.SrcOperands[1], WWWW_MASK);

            if (!inputsCorrect)
            {
                continue;
            }

            // make replacement

            UsilOperand mulInputVec4Operand = new UsilOperand(inst0.SrcOperands[0]);
            UsilOperand mulInputMat4x4Operand = new UsilOperand(inst0.SrcOperands[1]);
            UsilOperand mulOutputOperand = new UsilOperand(inst3.DestOperand);

            mulInputMat4x4Operand.DisplayMask = false;
            mulInputVec4Operand.Mask = new int[] { 0, 1, 2, 3 };

            UsilInstruction mulInstruction = new UsilInstruction()
            {
                InstructionType = UsilInstructionType.MultiplyMatrixByVector,
                DestOperand = mulOutputOperand,
                SrcOperands = new List<UsilOperand> { mulInputMat4x4Operand, mulInputVec4Operand }
            };

            insts.RemoveRange(i, 4);
            insts.Insert(i, mulInstruction);

            changes = true;
        }
        return changes;
    }

    // mat3x3 * vec3
    private static bool ReplaceMulMatrixVec3(UShaderProgram shader)
    {

        bool changes = false;

        List<UsilInstruction> insts = shader.Instructions;
        for (int i = 0; i < insts.Count - 3; i++)
        {
            // do detection

            bool opcodesMatch = DoOpcodesMatch(insts, i, new[] {
                UsilInstructionType.Multiply,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.MultiplyAdd,
                UsilInstructionType.Add
            });

            if (!opcodesMatch)
            {
                continue;
            }

            UsilInstruction inst0 = insts[i];
            UsilInstruction inst1 = insts[i + 1];
            UsilInstruction inst2 = insts[i + 2];
            UsilInstruction inst3 = insts[i + 3];

            bool matricesCorrect =
                inst0.SrcOperands[1].OperandType == UsilOperandType.Matrix &&
                inst0.SrcOperands[1].ArrayIndex == 1 &&
                DoMasksMatch(inst0.SrcOperands[1], XYZ_MASK) &&

                inst1.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst1.SrcOperands[0].ArrayIndex == 0 &&
                DoMasksMatch(inst1.SrcOperands[0], XYZ_MASK) &&

                inst2.SrcOperands[0].OperandType == UsilOperandType.Matrix &&
                inst2.SrcOperands[0].ArrayIndex == 2 &&
                DoMasksMatch(inst2.SrcOperands[0], XYZ_MASK) &&

                inst3.SrcOperands[1].OperandType == UsilOperandType.Matrix &&
                inst3.SrcOperands[1].ArrayIndex == 3 &&
                DoMasksMatch(inst3.SrcOperands[1], XYZ_MASK);

            if (!matricesCorrect)
            {
                continue;
            }

            int tmp0Index = inst0.DestOperand.RegisterIndex;
            int tmp1Index = inst1.DestOperand.RegisterIndex;
            int tmp2Index = inst2.DestOperand.RegisterIndex;
            int tmp3Index = inst3.DestOperand.RegisterIndex;

            // registers can swap halfway through to be used for something else
            // don't try to convert the matrix because we can't handle this yet
            if (tmp0Index != tmp1Index || tmp1Index != tmp2Index || tmp2Index != tmp3Index)
            {
                continue;
            }

            bool tempRegisterCorrect =
                inst0.DestOperand.RegisterIndex == tmp0Index &&
                inst1.DestOperand.RegisterIndex == tmp0Index &&
                inst1.SrcOperands[2].RegisterIndex == tmp0Index &&
                inst2.SrcOperands[2].RegisterIndex == tmp0Index &&

                inst2.DestOperand.RegisterIndex == tmp1Index &&
                inst3.SrcOperands[0].RegisterIndex == tmp1Index;

            if (!tempRegisterCorrect)
            {
                continue;
            }

            // todo: input isn't guaranteed temp
            // todo: is input guaranteed to start at x?
            int inpIndex = inst0.SrcOperands[0].RegisterIndex;
            bool inputsCorrect =
                inst0.SrcOperands[0].RegisterIndex == inpIndex &&
                DoMasksMatch(inst0.SrcOperands[0], YYY_MASK) &&

                inst1.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst1.SrcOperands[1], XXX_MASK) &&

                inst2.SrcOperands[1].RegisterIndex == inpIndex &&
                DoMasksMatch(inst2.SrcOperands[1], ZZZ_MASK);

            if (!inputsCorrect)
            {
                continue;
            }

            // make replacement

            UsilOperand mulInputVec3Operand = new UsilOperand(inst0.SrcOperands[0]);
            UsilOperand mulInputMat3x3Operand = new UsilOperand(inst0.SrcOperands[1]);
            UsilOperand mulOutputOperand = new UsilOperand(inst3.DestOperand);

            mulInputMat3x3Operand.DisplayMask = false;
            mulInputVec3Operand.Mask = new int[] { 0, 1, 2 };

            UsilInstruction mulInstruction = new UsilInstruction()
            {
                InstructionType = UsilInstructionType.MultiplyMatrixByVector,
                DestOperand = mulOutputOperand,
                SrcOperands = new List<UsilOperand> { mulInputMat3x3Operand, mulInputVec3Operand }
            };

            insts.RemoveRange(i, 4);
            insts.Insert(i, mulInstruction);

            changes = true;
        }
        return changes;
    }
}
