using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using USCSandbox.Processor;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Fixers
{
    /// <summary>
    /// Combines ResourceDimensionInfo and SampleCountInfo into a single GetDimensions call
    /// </summary>
    public class USILGetDimensionsFixer : IUSILOptimizer
    {
        public bool Run(UShaderProgram shader, ShaderParams shaderParams)
        {
            bool changes = false;

            List<USILInstruction> instructions = shader.instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                bool matches = USILOptimizerUtil.DoOpcodesMatch(instructions, i, new[]
                {
                    USILInstructionType.ResourceDimensionInfo,
                    USILInstructionType.SampleCountInfo
                });

                if (!matches)
                {
                    if (instructions[i].instructionType == USILInstructionType.ResourceDimensionInfo)
                    {
                        // discard unused NumberOfLevels for GetDimensions
                        if (!shader.locals.Any(l => l.name == "resinfo_extra"))
                        {
                            shader.locals.Add(new USILLocal("float", "resinfo_extra", USILLocalType.Scalar));
                            changes = true;
                        }
                    }
                    continue;
                }

                USILInstruction resinfoInst = instructions[0];
                USILInstruction sampleinfoInst = instructions[1];

                // needed? (did I even get the right registers?)
                if (resinfoInst.srcOperands[1].registerIndex != sampleinfoInst.srcOperands[0].registerIndex)
                {
                    continue;
                }

                resinfoInst.srcOperands[5] = sampleinfoInst.destOperand;

                instructions.RemoveAt(i + 1); // remove SampleCountInfo
                changes = true;
            }

            return changes; // any changes made?
        }
    }
}
