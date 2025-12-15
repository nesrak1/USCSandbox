namespace USCSandbox.ShaderCode.USIL;
public class UsilLocal
{
    public string Type;
    public string Name;
    public UsilLocalType UsilType;
    public bool IsArray;
    public List<UsilOperand> DefaultValues;

    public UsilLocal(string type, string name, UsilLocalType usilType, bool isArray = false)
    {
        Type = type;
        Name = name;
        UsilType = usilType;
        IsArray = isArray;
        DefaultValues = [];
    }
}
