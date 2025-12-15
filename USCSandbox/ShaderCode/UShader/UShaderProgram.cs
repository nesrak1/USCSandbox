using USCSandbox.ShaderCode.USIL;

namespace USCSandbox.ShaderCode.UShader;
public class UShaderProgram
{
    public UShaderFunctionType ShaderFunctionType;
    public List<UsilLocal> Locals;
    public List<UsilInstruction> Instructions;
    public List<UsilInputOutput> Inputs;
    public List<UsilInputOutput> Outputs;

    public UShaderProgram()
    {
        ShaderFunctionType = UShaderFunctionType.Unknown;
        Locals = new List<UsilLocal>();
        Instructions = new List<UsilInstruction>();
        Inputs = new List<UsilInputOutput>();
        Outputs = new List<UsilInputOutput>();
    }
}
