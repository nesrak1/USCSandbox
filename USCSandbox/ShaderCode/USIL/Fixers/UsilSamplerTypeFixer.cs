using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Fixers;
/// <summary>
/// Corrects the sampler type for built in types
/// </summary>
public class USILSamplerTypeFixer : IUsilOptimizer
{
    // There's most likely a better way to handle this, but I don't care right now.
    public static readonly HashSet<string?> BUILTIN_SAMPLER_TEXTURE_NAMES = new()
    {
        "unity_Lightmap",
        "unity_ShadowMask",
        "unity_DynamicLightmap",
        "unity_SpecCube0",
        "unity_ProbeVolumeSH"
    };

    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            if (instruction.IsSampleType())
            {
                UsilOperand sampleOperand = instruction.SrcOperands[2];

                // USILSamplerMetadder couldn't find sampler metadata, skip
                if (sampleOperand.OperandType == UsilOperandType.SamplerRegister)
                {
                    break;
                }

                // Shouldn't happen, but just in case
                if (!sampleOperand.MetadataNameAssigned)
                {
                    break;
                }

                if (BUILTIN_SAMPLER_TEXTURE_NAMES.Contains(sampleOperand.MetadataName))
                {
                    int samplerTypeIdx = GetSamplerTypeIdx(instruction.InstructionType);
                    if (samplerTypeIdx != -1)
                    {
                        instruction.SrcOperands[samplerTypeIdx] = new UsilOperand(1);
                        changes = true;
                    }
                }
            }
        }
        return changes; // any changes made?
    }

    private static int GetSamplerTypeIdx(UsilInstructionType type)
    {
        return type switch
        {
            UsilInstructionType.Sample => 3,
            UsilInstructionType.SampleLODBias => 4,
            UsilInstructionType.SampleComparison => 4,
            UsilInstructionType.SampleComparisonLODZero => 4,
            UsilInstructionType.SampleLOD => 4,
            _ => -1
        };
    }
}
