using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedShaderFloatValue
{
    public float Value;
    public string Name;

    public SerializedShaderFloatValue(AssetTypeValueField field)
    {
        Value = field["val"].AsFloat;
        Name = field["name"].AsString;
    }
}

public class SerializedShaderFloatValue<T>
    where T : Enum
{
    public T Value;
    public string Name;

    public SerializedShaderFloatValue(AssetTypeValueField field)
    {
        Value = (T)Enum.ToObject(typeof(T), (int)field["val"].AsFloat);
        Name = field["name"].AsString;
    }
}
