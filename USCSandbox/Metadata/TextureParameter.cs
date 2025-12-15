using AssetsTools.NET;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.Metadata;
public class TextureParameter
{
    public string Name;
    public int Index;
    public int SamplerIndex;
    public bool MultiSampled;
    public byte Dim;

    public TextureParameter(AssetsFileReader r, UnityVersion engVer, string name)
    {
        var index = r.ReadInt32();
        var extraValue = r.ReadInt32();

        Name = name;
        Index = index;

        var hasNewTextureParams = engVer.GreaterThanOrEquals(2018, 2);
        var hasMultiSampled = engVer.GreaterThanOrEquals(2017, 3);
        if (hasNewTextureParams)
        {
            var textureExtraValue = r.ReadUInt32();
            MultiSampled = (textureExtraValue & 1) == 1;
            Dim = (byte)(textureExtraValue >> 1);
            SamplerIndex = extraValue;
        }
        else if (hasMultiSampled)
        {
            var textureExtraValue = r.ReadUInt32();
            MultiSampled = textureExtraValue == 1;
            Dim = unchecked((byte)extraValue);
            SamplerIndex = extraValue >> 8;
            if (SamplerIndex == 0xFFFFFF)
            {
                SamplerIndex = -1;
            }
        }
        else
        {
            MultiSampled = false;
            Dim = unchecked((byte)extraValue);
            SamplerIndex = extraValue >> 8;
            if (SamplerIndex == 0xFFFFFF)
            {
                SamplerIndex = -1;
            }
        }
    }

    public TextureParameter(AssetTypeValueField field, Dictionary<int, string> nameTable)
    {
        Name = nameTable[field["m_NameIndex"].AsInt];
        Index = field["m_Index"].AsInt;
        SamplerIndex = field["m_SamplerIndex"].AsInt;
        MultiSampled = field["m_MultiSampled"].AsBool;
        Dim = (byte)field["m_Dim"].AsSByte;
    }
}
