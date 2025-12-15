using USCSandbox.Metadata;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Metadders;
public class UsilSamplerMetadder : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            foreach (UsilOperand operand in instruction.SrcOperands)
            {
                if (operand.OperandType == UsilOperandType.SamplerRegister)
                {
                    TextureParameter? texParam = shaderParams.TextureParameters.FirstOrDefault(
                        p => p.SamplerIndex == operand.RegisterIndex
                    );

                    if (texParam == null)
                    {
                        // fallback to -1 if it exists
                        texParam = shaderParams.TextureParameters.FirstOrDefault(
                            p => p.SamplerIndex == -1
                        );

                        if (texParam == null)
                        {
                            operand.OperandType = UsilOperandType.Sampler2D;
                            Console.WriteLine($"[WARN] Could not find texture parameter for sampler {operand}");
                            continue;
                        }
                    }

                    int dimension = texParam.Dim;
                    switch (dimension)
                    {
                        case 2:
                            operand.OperandType = UsilOperandType.Sampler2D;
                            break;
                        case 3:
                            operand.OperandType = UsilOperandType.Sampler3D;
                            break;
                        case 4:
                            operand.OperandType = UsilOperandType.SamplerCube;
                            break;
                        case 5:
                            operand.OperandType = UsilOperandType.Sampler2DArray;
                            break;
                        case 6:
                            operand.OperandType = UsilOperandType.SamplerCubeArray;
                            break;
                    }

                    if (texParam != null)
                    {
                        operand.MetadataName = texParam.Name;
                        operand.MetadataNameAssigned = true;
                    }
                }
                else if (operand.OperandType == UsilOperandType.ResourceRegister)
                {
                    TextureParameter? texParam = shaderParams.TextureParameters.FirstOrDefault(
                        p => p.Index == operand.RegisterIndex
                    );

                    if (texParam == null)
                    {
                        Console.WriteLine($"[WARN] Could not find texture parameter for resource {operand}");
                        continue;
                    }

                    if (texParam != null)
                    {
                        operand.MetadataName = texParam.Name;
                        operand.MetadataNameAssigned = true;
                    }
                }
            }
        }
        return true; // any changes made?
    }
}
