using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Fixers;
/// <summary>
/// Combines ResourceDimensionInfo and SampleCountInfo into a single GetDimensions call
/// </summary>
public class UsilGetDimensionsFixer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        List<UsilInstruction> instructions = shader.Instructions;
        for (int i = 0; i < instructions.Count; i++)
        {
            bool matches = UsilOptimizerUtil.DoOpcodesMatch(instructions, i, new[]
            {
                UsilInstructionType.ResourceDimensionInfo,
                UsilInstructionType.SampleCountInfo
            });

            if (!matches)
            {
                if (instructions[i].InstructionType == UsilInstructionType.ResourceDimensionInfo)
                {
                    // discard unused NumberOfLevels for GetDimensions
                    if (!shader.Locals.Any(l => l.Name == "resinfo_extra"))
                    {
                        shader.Locals.Add(new UsilLocal("float", "resinfo_extra", UsilLocalType.Scalar));
                        changes = true;
                    }
                }
                continue;
            }

            UsilInstruction resinfoInst = instructions[0];
            UsilInstruction sampleinfoInst = instructions[1];

            // needed? (did I even get the right registers?)
            if (resinfoInst.SrcOperands[1].RegisterIndex != sampleinfoInst.SrcOperands[0].RegisterIndex)
            {
                continue;
            }

            resinfoInst.SrcOperands[5] = sampleinfoInst.DestOperand;

            instructions.RemoveAt(i + 1); // remove SampleCountInfo
            changes = true;
        }

        return changes; // any changes made?
    }
}
