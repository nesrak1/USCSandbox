using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Optimizers;
/// <summary>
/// Moves constant values to the right in comparison instructions
/// </summary>
public class UsilCompareOrderOptimizer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            if (instruction.IsComparisonType())
            {
                UsilOperand leftOperand = instruction.SrcOperands[0];
                UsilOperand rightOperand = instruction.SrcOperands[1];
                bool leftIsConstant = IsOperandFullyConstant(leftOperand);
                bool rightIsConstant = IsOperandFullyConstant(rightOperand);
                if (leftIsConstant && !rightIsConstant)
                {
                    instruction.SrcOperands[0] = rightOperand;
                    instruction.SrcOperands[1] = leftOperand;
                    instruction.InstructionType = FlipCompareType(instruction.InstructionType);
                    changes = true;
                }
            }
        }
        return changes; // any changes made?
    }

    private static bool IsOperandFullyConstant(UsilOperand operand)
    {
        if (operand.OperandType is UsilOperandType.ImmediateInt or UsilOperandType.ImmediateFloat)
        {
            return true;
        }
        else if (operand.OperandType is UsilOperandType.Multiple)
        {
            bool fullyConstant = true;
            foreach (UsilOperand child in operand.Children)
            {
                fullyConstant &= IsOperandFullyConstant(child);
            }
        }
        return false;
    }

    private static UsilInstructionType FlipCompareType(UsilInstructionType type)
    {
        return type switch
        {
            UsilInstructionType.LessThan => UsilInstructionType.GreaterThan,
            UsilInstructionType.GreaterThan => UsilInstructionType.LessThan,
            UsilInstructionType.LessThanOrEqual => UsilInstructionType.GreaterThanOrEqual,
            UsilInstructionType.GreaterThanOrEqual => UsilInstructionType.LessThanOrEqual,
            _ => type,
        };
    }
}
