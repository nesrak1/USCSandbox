using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using USCSandbox.Processor;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Fixers
{
    /// <summary>
    /// Combines ResourceDimensionInfo and SampleCountInfo into a single GetDimensions call
    /// </summary>
    public class USILObviousUIntFixer : IUSILOptimizer
    {
        public bool Run(UShaderProgram shader, ShaderParams shaderParams)
        {
            bool changes = false;

            List<USILInstruction> instructions = shader.instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                USILInstruction instruction = instructions[i];
                if (instruction.instructionType != USILInstructionType.And &&
                    instruction.instructionType != USILInstructionType.Or &&
                    instruction.instructionType != USILInstructionType.Xor &&
                    instruction.instructionType != USILInstructionType.Not)
                {
                    continue;
                }

                foreach (USILOperand operand in instruction.srcOperands)
                {
                    if (operand.operandType == USILOperandType.ImmediateFloat)
                    {
                        int count = operand.immValueFloat.Length;
                        operand.immValueInt = new int[count];
                        for (int j = 0; j < count; j++)
                        {
                            //int intValue = BitConverter.SingleToInt32Bits(operand.immValueFloat[j]);
                            operand.immValueInt[j] = (int)operand.immValueFloat[j];
                        }
                        operand.operandType = USILOperandType.ImmediateInt;
                    }
                }
            }

            return changes; // any changes made?
        }
    }
}
