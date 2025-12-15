using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedStencilOp
{
    public SerializedShaderFloatValue<StencilOp> Pass;
    public SerializedShaderFloatValue<StencilOp> Fail;
    public SerializedShaderFloatValue<StencilOp> ZFail;
    public SerializedShaderFloatValue<StencilComp> Comp;

    public SerializedStencilOp(AssetTypeValueField field)
    {
        Pass = new SerializedShaderFloatValue<StencilOp>(field["pass"]);
        Fail = new SerializedShaderFloatValue<StencilOp>(field["fail"]);
        ZFail = new SerializedShaderFloatValue<StencilOp>(field["zFail"]);
        Comp = new SerializedShaderFloatValue<StencilComp>(field["comp"]);
    }
}
