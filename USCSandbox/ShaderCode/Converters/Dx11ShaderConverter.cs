using AssetsTools.NET;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using USCSandbox.ShaderCode.Converters.DirectXDisassembler;
using USCSandbox.ShaderCode.Converters.ToUsil;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderCode.Converters;
public static class Dx11ShaderConverter
{
    public static List<Dx11ShaderSubprogram> Convert(SerializedPass pass, BlobManager blobMan, UnityVersion version)
    {
        var subProgs = new List<Dx11ShaderSubprogram>();

        foreach (var program in pass.Programs)
        {
            var subProgInfs = program.SubProgramInfos
                .Where(i => IsSupportedProgram(i.GpuProgramType))
                .ToArray();

            if (subProgInfs.Length < 1)
                continue;

            var subProgInf = subProgInfs[0];

            // get subprogram header (data) and dxbc bytes (data stream)
            var subProgData = blobMan.GetShaderSubProgram((int)subProgInf.BlobIndex);
            var subProgDataStream = new MemoryStream(subProgData.ProgramData);

            // parse dxbc shader
            var offset = GetDirectXDataOffset(version, GPUPlatform.d3d11, subProgDataStream.ReadByte());
            var trimmedData = new SegmentStream(subProgDataStream, offset);
            var dxShader = new DirectXCompiledShader(trimmedData);

            // convert to usil
            var dx2UsilConverter = new DirectXProgramToUsil(dxShader);
            dx2UsilConverter.Convert();

            // get shader and params
            var uShaderProg = dx2UsilConverter.Shader;
            var shaderParams = subProgInf.UsesParameterBlob
                ? blobMan.GetShaderParams((int)subProgInf.ParameterBlobIndex)
                : subProgData.ShaderParams!; //! we would have thrown if this was null by now

            if (program.CommonParams is { } commonParams)
            {
                shaderParams.CombineCommon(commonParams);
            }

            // apply metadata to ushader
            ApplyMetadataToProgram(uShaderProg, subProgData, shaderParams, version);

            // save decompiled shader into dict
            var funcType = uShaderProg.ShaderFunctionType;
            var comboKeywords = subProgData.GlobalKeywords.Concat(subProgData.LocalKeywords)
                .Order()
                .ToArray();

            subProgs.Add(new Dx11ShaderSubprogram(dxShader, shaderParams, uShaderProg, comboKeywords));
        }

        return subProgs;
    }

    private static void ApplyMetadataToProgram(
        UShaderProgram uShaderProg, ShaderSubProgramData subProgram,
        ShaderParameters shaderParams, UnityVersion version)
    {
        var shaderProgType = subProgram.GetProgramType(version);
        var shaderFuncType = GetUShaderFunctionType(shaderProgType);
        if (shaderFuncType == UShaderFunctionType.Unknown)
        {
            throw new NotSupportedException("Only vertex and fragment shaders are supported at the moment");
        }

        uShaderProg.ShaderFunctionType = shaderFuncType;

        UsilOptimizerApplier.Apply(uShaderProg, shaderParams);
    }

    private static int GetDirectXDataOffset(UnityVersion version, GPUPlatform graphicApi, int headerVersion)
    {
        // this check is slightly useless because we onnly support dx11 right now :3
        bool hasHeader = graphicApi != GPUPlatform.d3d9;
        if (hasHeader)
        {
            bool hasGSInputPrimitive = version.GreaterThanOrEquals(5, 4);
            int offset = hasGSInputPrimitive ? 6 : 5;
            if (headerVersion >= 2)
            {
                offset += 0x20;
            }

            return offset;
        }
        else
        {
            return 0;
        }
    }

    private static bool IsSupportedProgram(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.DX11VertexSM40 or
            ShaderGpuProgramType.DX11PixelSM40 or
            ShaderGpuProgramType.DX11VertexSM50 or
            ShaderGpuProgramType.DX11PixelSM50 => true,
            _ => false,
        };
    }

    private static UShaderFunctionType GetUShaderFunctionType(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.DX11VertexSM40 or
            ShaderGpuProgramType.DX11VertexSM50 => UShaderFunctionType.Vertex,
            ShaderGpuProgramType.DX11PixelSM40 or
            ShaderGpuProgramType.DX11PixelSM50 => UShaderFunctionType.Fragment,
            _ => UShaderFunctionType.Unknown,
        };
    }

    public class Dx11ShaderSubprogram
    {
        public DirectXCompiledShader DxShader;
        public ShaderParameters Parameters;
        public UShaderProgram UShaderProg;
        public string[] Keywords;

        public Dx11ShaderSubprogram(
            DirectXCompiledShader dxShader, ShaderParameters parameters,
            UShaderProgram uShaderProg, string[] keywords)
        {
            DxShader = dxShader;
            Parameters = parameters;
            UShaderProg = uShaderProg;
            Keywords = keywords;
        }
    }
}
