using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedShaderState
{
    public string Name;
    public List<SerializedShaderRTBlendState> RtBlendState;
    public SerializedShaderFloatValue<ZClip> ZClip;
    public SerializedShaderFloatValue<ZTest> ZTest;
    public SerializedShaderFloatValue<ZWrite> ZWrite;
    public SerializedShaderFloatValue<CullMode> Culling;
    public SerializedShaderFloatValue? Conservative; // not in 2019.4 game
    public SerializedShaderFloatValue OffsetFactor;
    public SerializedShaderFloatValue OffsetUnits;
    public SerializedShaderFloatValue AlphaToMask;
    public SerializedStencilOp StencilOp;
    public SerializedStencilOp StencilOpFront;
    public SerializedStencilOp StencilOpBack;
    public SerializedShaderFloatValue StencilReadMask;
    public SerializedShaderFloatValue StencilWriteMask;
    public SerializedShaderFloatValue StencilRef;
    public SerializedShaderFloatValue FogStart;
    public SerializedShaderFloatValue FogEnd;
    public SerializedShaderFloatValue FogDensity;
    public SerializedShaderVectorValue FogColor;
    public FogMode FogMode;
    public Dictionary<string, string> Tags;
    public int LOD;
    public bool Lighting;

    public SerializedShaderState(AssetTypeValueField field)
    {
        Name = field["m_Name"].AsString;

        if (field["rtSeparateBlend"].AsBool)
        {
            RtBlendState = new List<SerializedShaderRTBlendState>(8);
            for (int i = 0; i < 8; i++)
            {
                RtBlendState[i] = new SerializedShaderRTBlendState(field["rtBlend" + i]);
            }
        }
        else
        {
            RtBlendState = [
                new SerializedShaderRTBlendState(field["rtBlend0"])
            ];
        }

        ZClip = new SerializedShaderFloatValue<ZClip>(field["zClip"]);
        ZTest = new SerializedShaderFloatValue<ZTest>(field["zTest"]);
        ZWrite = new SerializedShaderFloatValue<ZWrite>(field["zWrite"]);
        Culling = new SerializedShaderFloatValue<CullMode>(field["culling"]);
        Conservative = !field["conservative"].IsDummy
            ? new SerializedShaderFloatValue(field["conservative"])
            : null;

        OffsetFactor = new SerializedShaderFloatValue(field["offsetFactor"]);
        OffsetUnits = new SerializedShaderFloatValue(field["offsetUnits"]);
        AlphaToMask = new SerializedShaderFloatValue(field["alphaToMask"]);
        StencilOp = new SerializedStencilOp(field["stencilOp"]);
        StencilOpFront = new SerializedStencilOp(field["stencilOpFront"]);
        StencilOpBack = new SerializedStencilOp(field["stencilOpBack"]);
        StencilReadMask = new SerializedShaderFloatValue(field["stencilReadMask"]);
        StencilWriteMask = new SerializedShaderFloatValue(field["stencilWriteMask"]);
        StencilRef = new SerializedShaderFloatValue(field["stencilRef"]);
        FogStart = new SerializedShaderFloatValue(field["fogStart"]);
        FogEnd = new SerializedShaderFloatValue(field["fogEnd"]);
        FogDensity = new SerializedShaderFloatValue(field["fogDensity"]);
        FogColor = new SerializedShaderVectorValue(field["fogColor"]);
        FogMode = (FogMode)(int)field["fogMode"].AsFloat;
        Tags = field["m_Tags.tags.Array"]
            .ToDictionary(ni => ni[0].AsString, ni => ni[1].AsString);

        LOD = field["m_LOD"].AsInt;
        Lighting = field["lighting"].AsBool;
    }
}
