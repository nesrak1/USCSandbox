using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL;
public interface IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderData);
}
