﻿using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using USCSandbox;
using USCSandbox.Processor;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Metadders
{
    public class USILSamplerMetadder : IUSILOptimizer
    {
        public bool Run(UShaderProgram shader, ShaderParams shaderParams)
        {
            List<USILInstruction> instructions = shader.instructions;
            foreach (USILInstruction instruction in instructions)
            {
                foreach (USILOperand operand in instruction.srcOperands)
                {
                    if (operand.operandType == USILOperandType.SamplerRegister)
                    {
                        TextureParameter? texParam = shaderParams.TextureParameters.FirstOrDefault(
                            p => p.SamplerIndex == operand.registerIndex
                        );

                        if (texParam == null)
                        {
                            // fallback to -1 if it exists
                            texParam = shaderParams.TextureParameters.FirstOrDefault(
                                p => p.SamplerIndex == -1
                            );
                            
                            if (texParam == null)
                            {
                                operand.operandType = USILOperandType.Sampler2D;
                                Logger.Warning($"Could not find texture parameter for sampler {operand}");
                                continue;
                            }
                        }

                        int dimension = texParam.Dim;
                        switch (dimension)
                        {
                            case 2:
                                operand.operandType = USILOperandType.Sampler2D;
                                break;
                            case 3:
                                operand.operandType = USILOperandType.Sampler3D;
                                break;
                            case 4:
                                operand.operandType = USILOperandType.SamplerCube;
                                break;
                            case 5:
                                operand.operandType = USILOperandType.Sampler2DArray;
                                break;
                            case 6:
                                operand.operandType = USILOperandType.SamplerCubeArray;
                                break;
                        }

                        if (texParam != null)
                        {
                            operand.metadataName = texParam.Name;
                            operand.metadataNameAssigned = true;
                        }
                    }
                    else if (operand.operandType == USILOperandType.ResourceRegister)
                    {
                        TextureParameter? texParam = shaderParams.TextureParameters.FirstOrDefault(
                            p => p.Index == operand.registerIndex
                        );

                        if (texParam == null)
                        {
                            Logger.Warning($"Could not find texture parameter for resource {operand}");
                            continue;
                        }

                        if (texParam != null)
                        {
                            operand.metadataName = texParam.Name;
                            operand.metadataNameAssigned = true;
                        }
                    }
                }
            }
            return true; // any changes made?
        }
    }
}
