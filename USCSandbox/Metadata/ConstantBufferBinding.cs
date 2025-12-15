using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class ConstantBufferBinding
{
    public string Name;
    public int Index;
    public int ArraySize;

    public ConstantBufferBinding(AssetsFileReader r, string name)
    {
        Name = name;
        Index = r.ReadInt32();
        ArraySize = r.ReadInt32();
    }

    public ConstantBufferBinding(AssetTypeValueField field, Dictionary<int, string> nameTable)
    {
        Name = nameTable[field["m_NameIndex"].AsInt];
        Index = field["m_Index"].AsInt;
        ArraySize = field["m_ArraySize"].AsInt;
    }
}