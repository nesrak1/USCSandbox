using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedShaderVectorValue
{
    public SerializedShaderFloatValue X;
    public SerializedShaderFloatValue Y;
    public SerializedShaderFloatValue Z;
    public SerializedShaderFloatValue W;

    public SerializedShaderVectorValue(AssetTypeValueField field)
    {
        X = new SerializedShaderFloatValue(field["x"]);
        Y = new SerializedShaderFloatValue(field["y"]);
        Z = new SerializedShaderFloatValue(field["z"]);
        W = new SerializedShaderFloatValue(field["w"]);
    }
}
