using AssetsTools.NET;

namespace USCSandbox.Metadata;
// this class appears in the serialized asset data.
// for the data that appears after the base shader in a shader blob, see ShaderParameters.
public class SerializedProgramParameters
{
    public List<int> Vectors;
    public List<int> Matrices;
    public List<TextureParameter> Textures;
    public List<int> Buffers;
    public List<ConstantBuffer> CBuffers;
    public List<ConstantBufferBinding> CBufferBindings;
    public List<int> UAVParams;
    public List<int> Samplers;

    public SerializedProgramParameters(AssetTypeValueField field, Dictionary<int, string> nameTable)
    {
        // this will error out if we hit any unsupported fields (.AsInt ones) since none will be int
        Vectors = SerializedMetadataHelpers.GetArrayFirstValue(field["m_VectorParams.Array"])
            .Select(p => p.AsInt).ToList();
        Matrices = SerializedMetadataHelpers.GetArrayFirstValue(field["m_MatrixParams.Array"])
            .Select(p => p.AsInt).ToList();
        Textures = SerializedMetadataHelpers.GetArrayFirstValue(field["m_TextureParams.Array"])
            .Select(p => new TextureParameter(p, nameTable)).ToList();
        Buffers = SerializedMetadataHelpers.GetArrayFirstValue(field["m_BufferParams.Array"])
            .Select(p => p.AsInt).ToList();
        CBuffers = SerializedMetadataHelpers.GetArrayFirstValue(field["m_ConstantBuffers.Array"])
            .Select(p => new ConstantBuffer(p, nameTable)).ToList();
        CBufferBindings = SerializedMetadataHelpers.GetArrayFirstValue(field["m_ConstantBufferBindings.Array"])
            .Select(p => new ConstantBufferBinding(p, nameTable)).ToList();
        UAVParams = SerializedMetadataHelpers.GetArrayFirstValue(field["m_UAVParams.Array"])
            .Select(p => p.AsInt).ToList();
        Samplers = SerializedMetadataHelpers.GetArrayFirstValue(field["m_Samplers.Array"])
            .Select(p => p[0].AsInt).ToList();
    }
}
