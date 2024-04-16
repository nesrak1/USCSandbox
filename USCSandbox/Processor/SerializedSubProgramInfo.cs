using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class SerializedSubProgramInfo
    {
        public List<ushort> KeywordIndices;
        public sbyte GpuProgramType;
        public uint BlobIndex;

        public SerializedSubProgramInfo(AssetTypeValueField field)
        {
            KeywordIndices = field["m_KeywordIndices.Array"].Select(i => i.AsUShort).ToList();
            GpuProgramType = field["m_GpuProgramType"].AsSByte;
            BlobIndex = field["m_BlobIndex"].AsUInt;
        }
    }
}