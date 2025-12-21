using AssetsTools.NET;
using USCSandbox.Common;

namespace USCSandbox.Metadata;
public class SerializedSubProgram
{
    public List<ushort> KeywordIndices;
    public ShaderGpuProgramType GpuProgramType;
    public uint BlobIndex;
    public uint ParameterBlobIndex;

    public SerializedProgramParameters? Params;

    public bool UsesParameterBlob => ParameterBlobIndex != uint.MaxValue;

    public SerializedSubProgram(AssetTypeValueField field, Dictionary<int, string> nameTable, uint paramBlobIdx = uint.MaxValue)
    {
        KeywordIndices = field["m_KeywordIndices.Array"].Select(i => i.AsUShort).ToList();
        GpuProgramType = (ShaderGpuProgramType)(int)field["m_GpuProgramType"].AsSByte;
        BlobIndex = field["m_BlobIndex"].AsUInt;
        ParameterBlobIndex = paramBlobIdx;

        if (!field["m_VectorParam"].IsDummy)
        {
            // seen in 2019.4 game
            Params = new SerializedProgramParameters(field, nameTable);
        }
        else if (!field["m_Parameters"].IsDummy)
        {
            // seen in 2021.3 game
            Params = new SerializedProgramParameters(field["m_Parameters"], nameTable);
        }
        else
        {
            // seen in 6.0 game
            // ... no params here, see ParameterBlob
            if (ParameterBlobIndex == uint.MaxValue)
            {
                throw new NotSupportedException(
                    "Either a ParameterBlobIndex has to be set or there " +
                    "have to be params. Somehow we have neither.");
            }
        }
    }
}
