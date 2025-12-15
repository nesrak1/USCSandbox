using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL.Fixers;
using USCSandbox.ShaderCode.USIL.Metadders;
using USCSandbox.ShaderCode.USIL.Optimizers;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL;
public static class UsilOptimizerApplier
{
    /// <summary>
    /// An array of optimizers to apply.
    /// </summary>
    /// <remarks>
    /// Order is important. Calling <see cref="IUsilOptimizer.Run(UShaderProgram, ShaderSubProgram)"/> should not modify
    /// the state of the optimizer.
    /// </remarks>
    private static readonly IUsilOptimizer[] OPTIMIZER_TYPES = new IUsilOptimizer[]
    {
        // do metadders first
        new UsilCBufferMetadder(),
        new UsilSamplerMetadder(),
        new UsilInputOutputMetadder(),
        
        // do fixes (you really should have these enabled!)
        new USILSamplerTypeFixer(),
        new UsilGetDimensionsFixer(),
        
        // do detection optimizers which usually depend on metadders
        //new USILMatrixMulOptimizer(), // I don't trust this code so it's commented for now

        // do simplification optimizers last when detection has been finished
        new UsilCompareOrderOptimizer(),
        new UsilAddNegativeOptimizer(),
        // new USILAndOptimizer(),
        // needs to be after AndOptimizer
        new USILObviousUIntFixer(),
        // //////////////////////////////
        new UsilForLoopOptimizer(),
    };

    public static void Apply(UShaderProgram shader, ShaderParameters shaderData)
    {
        for (int i = 0; i < OPTIMIZER_TYPES.Length; i++)
        {
            OPTIMIZER_TYPES[i].Run(shader, shaderData);
        }
    }
}
