using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Metadders;
public class UsilInputOutputMetadder : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            if (instruction.DestOperand != null)
            {
                UseMetadata(instruction.DestOperand, shader);
            }
            foreach (UsilOperand operand in instruction.SrcOperands)
            {
                UseMetadata(operand, shader);
            }
        }
        return true; // any changes made?
    }

    private static void UseMetadata(UsilOperand operand, UShaderProgram shader)
    {
        if (operand.OperandType == UsilOperandType.InputRegister)
        {
            int searchMask = operand.Mask.Length != 0 ? 1 << operand.Mask[0] : 0;
            UsilInputOutput? input = shader.Inputs.FirstOrDefault(
                i => i.Register == operand.RegisterIndex && (searchMask & i.Mask) == searchMask
            );

            // bail since we can't find the input
            if (input == null)
                return;

            // correct mask
            operand.Mask = MatchMaskToInputOutput(operand.Mask, input.Mask, true);

            if (shader.ShaderFunctionType == UShaderFunctionType.Fragment && input.Type == "SV_IsFrontFace")
            {
                operand.MetadataName = input.Name;
            }
            else
            {
                operand.MetadataName = shader.ShaderFunctionType switch
                {
                    UShaderFunctionType.Vertex => $"{UsilConstants.VERT_INPUT_NAME}.{input.Name}",
                    UShaderFunctionType.Fragment => $"{UsilConstants.FRAG_INPUT_NAME}.{input.Name}",
                    _ => $"unk_input.{input.Name}",
                };
            }

            operand.MetadataNameAssigned = true;
        }
        else if (operand.OperandType == UsilOperandType.OutputRegister)
        {
            int searchMask = 0;
            for (int i = 0; i < operand.Mask.Length; i++)
            {
                searchMask |= 1 << operand.Mask[i];
            }

            List<UsilInputOutput> outputs = shader.Outputs.Where(
                o => o.Register == operand.RegisterIndex && (searchMask & o.Mask) != 0
            ).ToList();

            foreach (UsilInputOutput output in outputs)
            {
                // correct mask
                int[] matchedMask = MatchMaskToInputOutput(operand.Mask, output.Mask, true);
                int[] realMatchedMask = MatchMaskToInputOutput(operand.Mask, output.Mask, false);
                operand.Mask = matchedMask;

                operand.MetadataName = shader.ShaderFunctionType switch
                {
                    UShaderFunctionType.Vertex => $"{UsilConstants.VERT_OUTPUT_LOCAL_NAME}.{output.Name}",
                    UShaderFunctionType.Fragment => $"{UsilConstants.FRAG_OUTPUT_LOCAL_NAME}.{output.Name}",
                    _ => $"unk_output.{output.Name}",
                };
                operand.MetadataNameAssigned = true;
            }
        }
        else if (HlslNamingUtils.HasSpecialInputOutputName(operand.OperandType))
        {
            string name = HlslNamingUtils.GetSpecialInputOutputName(operand.OperandType);

            operand.MetadataName = shader.ShaderFunctionType switch
            {
                UShaderFunctionType.Vertex => $"{UsilConstants.VERT_OUTPUT_LOCAL_NAME}.{name}",
                UShaderFunctionType.Fragment => $"{UsilConstants.FRAG_OUTPUT_LOCAL_NAME}.{name}",
                _ => $"unk_special.{name}",
            };
            operand.MetadataNameAssigned = true;
        }
    }

    private static int[] MatchMaskToInputOutput(int[] mask, int maskTest, bool moveSwizzles)
    {
        // Move swizzles (for example, .zw -> .xy) based on first letter
        int moveCount = 0;
        int i;
        for (i = 0; i < 4; i++)
        {
            if ((maskTest & 1) == 1)
            {
                break;
            }

            moveCount++;
            maskTest >>= 1;
        }

        // Count remaining 1 bits
        int bitCount = 0;
        for (; i < 4; i++)
        {
            if ((maskTest & 1) == 0)
            {
                break;
            }

            bitCount++;
            maskTest >>= 1;
        }

        List<int> result = new List<int>();
        for (int j = 0; j < mask.Length; j++)
        {
            if (mask[j] >= moveCount && mask[j] < bitCount + moveCount)
            {
                if (moveSwizzles)
                {
                    result.Add(mask[j] - moveCount);
                }
                else
                {
                    result.Add(mask[j]);
                }
            }
        }
        return result.ToArray();
    }
}
