using AssetsTools.NET;
using USCSandbox.Metadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderMetadata;
public class ShaderParameters
{
    public ConstantBuffer? BaseConstantBuffer;
    public List<ConstantBuffer> ConstantBuffers;

    public List<TextureParameter> TextureParameters;
    public List<ConstantBufferBinding> ConstBindings;
    public List<ConstantBufferBinding> Buffers;
    public List<UAVParameter> UAVs;
    public List<SamplerParameter> Samplers;

    public ShaderParameters(AssetsFileReader r, UnityVersion engVer, bool readBlobVersion)
    {
        if (readBlobVersion)
        {
            var blobVersion = r.ReadInt32();
        }
        var firstParamsCount = r.ReadInt32();
        if (firstParamsCount > 0)
        {
            BaseConstantBuffer = new ConstantBuffer(r, engVer);
            ConstantBuffers = new List<ConstantBuffer>(firstParamsCount - 1);
            for (var i = 1; i < firstParamsCount; i++)
            {
                ConstantBuffers.Add(new ConstantBuffer(r, engVer));
            }
        }
        else
        {
            ConstantBuffers = new List<ConstantBuffer>(0);
        }

        TextureParameters = new List<TextureParameter>();
        ConstBindings = new List<ConstantBufferBinding>();
        Buffers = new List<ConstantBufferBinding>();
        UAVs = new List<UAVParameter>();
        Samplers = new List<SamplerParameter>();

        var secondParamsCount = r.ReadInt32();
        for (var i = 0; i < secondParamsCount; i++)
        {
            var name = r.ReadCountStringInt32();
            r.Align();

            var type = r.ReadInt32();

            if (type == 0)
            {
                TextureParameters.Add(new TextureParameter(r, engVer, name));
            }
            else if (type == 1)
            {
                ConstBindings.Add(new ConstantBufferBinding(r, name));
            }
            else if (type == 2)
            {
                Buffers.Add(new ConstantBufferBinding(r, name));
            }
            else if (type == 3)
            {
                UAVs.Add(new UAVParameter(r, name));
            }
            else if (type == 4)
            {
                Samplers.Add(new SamplerParameter(r));
            }
        }
    }

    public void CombineCommon(SerializedProgramParameters progParams)
    {
        List<ConstantBuffer> commonCBuffers = progParams.CBuffers;
        List<ConstantBufferBinding> commonConstBindings = progParams.CBufferBindings;

        foreach (var commonCBuf in commonCBuffers)
        {
            if (commonCBuf.Partial)
            {
                var insertInto = ConstantBuffers.FirstOrDefault(c => c.Name == commonCBuf.Name);
                insertInto?.CBParams.AddRange(commonCBuf.CBParams);
            }
        }

        ConstBindings.AddRange(commonConstBindings);

        TextureParameters.AddRange(progParams.Textures);
    }
}