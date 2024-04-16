using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class SerializedProgramInfo
    {
        public List<uint> ParameterBlobIndices;
        public List<SerializedSubProgramInfo> SubProgramInfos;
        public List<TextureParameter> CommonTextureParameters;
        public List<ConstantBuffer> CommonCBuffers;
        public List<BufferBinding> CommonCBBindings;

        public SerializedProgramInfo(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            if (!field["m_PlayerSubPrograms"].IsDummy)
            {
                var parameterBlobIndices = field["m_ParameterBlobIndices.Array"];
                if (parameterBlobIndices.Children.Count > 0)
                {
                    ParameterBlobIndices = parameterBlobIndices
                        .Last()["Array"]
                        .Select(i => i.AsUInt)
                        .ToList();
                }
                else
                {
                    ParameterBlobIndices = new List<uint>(0);
                }

                var subProgramInfos = field["m_PlayerSubPrograms.Array"];
                if (subProgramInfos.Children.Count > 0)
                {
                    SubProgramInfos = subProgramInfos
                        .Last()["Array"]
                        .Select(i => new SerializedSubProgramInfo(i))
                        .ToList();
                }
                else
                {
                    SubProgramInfos = new List<SerializedSubProgramInfo>(0);
                }
            }
            else
            {
                ParameterBlobIndices = new List<uint>();
                SubProgramInfos = field["m_SubPrograms.Array"]
                    .Select(i => new SerializedSubProgramInfo(i))
                    .ToList();
            }

            if (!field["m_CommonParameters"].IsDummy)
            {
                var commonParams = field["m_CommonParameters"];
                CommonTextureParameters = GetCommonTextureParams(commonParams["m_TextureParams.Array"], nameTable);
                CommonCBuffers = GetCommonCBuffers(commonParams["m_ConstantBuffers.Array"], nameTable);
                CommonCBBindings = GetCommonCBBindings(commonParams["m_ConstantBufferBindings.Array"], nameTable);
            }
            else
            {
                CommonTextureParameters = new List<TextureParameter>();
                CommonCBuffers = new List<ConstantBuffer>();
                CommonCBBindings = new List<BufferBinding>();
            }
        }

        private List<TextureParameter> GetCommonTextureParams(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            var textureParams = new List<TextureParameter>();
            foreach (var param in field)
            {
                textureParams.Add(new TextureParameter(param, nameTable));
            }

            return textureParams;
        }

        private List<ConstantBuffer> GetCommonCBuffers(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            var cbuffers = new List<ConstantBuffer>();
            foreach (var cbuf in field)
            {
                cbuffers.Add(new ConstantBuffer(cbuf, nameTable));
            }

            return cbuffers;
        }

        private List<BufferBinding> GetCommonCBBindings(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            var bindings = new List<BufferBinding>();
            foreach (var binding in field)
            {
                bindings.Add(new BufferBinding(binding, nameTable));
            }

            return bindings;
        }

        public List<SerializedSubProgramInfo> GetForPlatform(int gpuProgramType)
        {
            return SubProgramInfos
                .Where(spi => spi.GpuProgramType == gpuProgramType)
                .ToList();
        }
    }
}