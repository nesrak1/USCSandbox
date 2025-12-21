using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Optimizers;
/// <summary>
/// Changes A + -B to A - B
/// </summary>
public class UsilAddNegativeOptimizer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            if (instruction.InstructionType == UsilInstructionType.Add)
            {
                UsilOperand leftOperand = instruction.SrcOperands[0];
                UsilOperand rightOperand = instruction.SrcOperands[1];
                if (IsTrulyNegative(rightOperand))
                {
                    instruction.InstructionType = UsilInstructionType.Subtract;
                    NegateOperand(rightOperand);
                    changes = true;
                }
                else if (IsTrulyNegative(leftOperand) && !IsTrulyNegative(rightOperand))
                {
                    instruction.InstructionType = UsilInstructionType.Subtract;
                    NegateOperand(leftOperand);
                    instruction.SrcOperands[0] = rightOperand;
                    instruction.SrcOperands[1] = leftOperand;
                    changes = true;
                }
            }
        }
        return changes; // any changes made?
    }

    private static bool IsTrulyNegative(UsilOperand operand)
    {
        switch (operand.OperandType)
        {
            case UsilOperandType.ImmediateInt:
            {
                foreach (int imm in operand.ImmInt)
                {
                    // this includes 0 as being ok for negative. hopefully there are no +/- 0 instructions?
                    if (imm > 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            case UsilOperandType.ImmediateFloat:
            {
                foreach (float imm in operand.ImmFloat)
                {
                    if (imm > 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            case UsilOperandType.Multiple:
            {
                foreach (UsilOperand child in operand.Children)
                {
                    if (!IsTrulyNegative(child))
                    {
                        return false;
                    }
                }
                return true;
            }

            default:
                return operand.Negative;
        }
    }

    private static void NegateOperand(UsilOperand operand)
    {
        switch (operand.OperandType)
        {
            case UsilOperandType.ImmediateInt:
            {
                for (int i = 0; i < operand.ImmInt.Length; i++)
                {
                    operand.ImmInt[i] = -operand.ImmInt[i];
                }

                break;
            }

            case UsilOperandType.ImmediateFloat:
            {
                for (int i = 0; i < operand.ImmFloat.Length; i++)
                {
                    operand.ImmFloat[i] = -operand.ImmFloat[i];
                }

                break;
            }

            case UsilOperandType.Multiple:
            {
                foreach (UsilOperand child in operand.Children)
                {
                    NegateOperand(child);
                }

                break;
            }

            default:
                operand.Negative = !operand.Negative;
                break;
        }
    }
}
