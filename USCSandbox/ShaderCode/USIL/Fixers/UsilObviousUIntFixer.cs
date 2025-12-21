using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Fixers;
/// <summary>
/// Converts a value (that we defaulted to float) to an integer
/// since we see it is obviously part of an integer operation.
/// </summary>
public class USILObviousUIntFixer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        List<UsilInstruction> instructions = shader.Instructions;
        for (int i = 0; i < instructions.Count; i++)
        {
            UsilInstruction instruction = instructions[i];
            if (instruction.InstructionType != UsilInstructionType.And &&
                instruction.InstructionType != UsilInstructionType.Or &&
                instruction.InstructionType != UsilInstructionType.Xor &&
                instruction.InstructionType != UsilInstructionType.Not)
            {
                continue;
            }

            foreach (UsilOperand operand in instruction.SrcOperands)
            {
                if (operand.OperandType == UsilOperandType.ImmediateFloat)
                {
                    int count = operand.ImmFloat.Length;
                    operand.ImmInt = new int[count];
                    for (int j = 0; j < count; j++)
                    {
                        //int intValue = BitConverter.SingleToInt32Bits(operand.immValueFloat[j]);
                        operand.ImmInt[j] = (int)operand.ImmFloat[j];
                    }
                    operand.OperandType = UsilOperandType.ImmediateInt;
                }
            }
        }

        return changes; // any changes made?
    }
}
