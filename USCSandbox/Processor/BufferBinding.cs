using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class BufferBinding
    {
        public string Name;
        public int Index;
        public int ArraySize;

        public BufferBinding(AssetsFileReader r, string name)
        {
            Name = name;
            Index = r.ReadInt32();
            ArraySize = r.ReadInt32();
        }

        public BufferBinding(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            Name = nameTable[field["m_NameIndex"].AsInt];
            Index = field["m_Index"].AsInt;
            ArraySize = field["m_ArraySize"].AsInt;
        }
    }
}