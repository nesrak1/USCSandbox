using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedShaderRTBlendState
{
    public SerializedShaderFloatValue<BlendMode> SrcBlend;
    public SerializedShaderFloatValue<BlendMode> DestBlend;
    public SerializedShaderFloatValue<BlendMode> SrcBlendAlpha;
    public SerializedShaderFloatValue<BlendMode> DestBlendAlpha;
    public SerializedShaderFloatValue<BlendOp> BlendOp;
    public SerializedShaderFloatValue<BlendOp> BlendOpAlpha;
    public SerializedShaderFloatValue<ColorWriteMask> ColMask;

    public SerializedShaderRTBlendState(AssetTypeValueField field)
    {
        SrcBlend = new SerializedShaderFloatValue<BlendMode>(field["srcBlend"]);
        DestBlend = new SerializedShaderFloatValue<BlendMode>(field["destBlend"]);
        SrcBlendAlpha = new SerializedShaderFloatValue<BlendMode>(field["srcBlendAlpha"]);
        DestBlendAlpha = new SerializedShaderFloatValue<BlendMode>(field["destBlendAlpha"]);
        BlendOp = new SerializedShaderFloatValue<BlendOp>(field["blendOp"]);
        BlendOpAlpha = new SerializedShaderFloatValue<BlendOp>(field["blendOpAlpha"]);
        ColMask = new SerializedShaderFloatValue<ColorWriteMask>(field["colMask"]);
    }
}
