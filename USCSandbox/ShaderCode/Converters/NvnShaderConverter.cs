using Ryujinx.Graphics.Shader.Translation;
using System.Buffers.Binary;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using USCSandbox.ShaderCode.Converters.NVN;
using USCSandbox.ShaderCode.Converters.ToUsil;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderCode.Converters;
public static class NvnShaderConverter
{
    public static List<NvnShaderSubprogram> Convert(SerializedPass pass, BlobManager blobMan, UnityVersion version)
    {
        var subProgs = new List<NvnShaderSubprogram>();

        foreach (var program in pass.Programs)
        {
            var subProgInfs = program.SubProgramInfos
                .Where(i => IsSupportedProgram(i.GpuProgramType))
                .ToArray();

            if (subProgInfs.Length < 1)
                continue;

            var subProgInf = subProgInfs[0];

            // get subprogram header (data) and nvn bytes (data stream)
            var subProgData = blobMan.GetShaderSubProgram((int)subProgInf.BlobIndex);
            var subProgDataStream = new MemoryStream(subProgData.ProgramData);

            // get params early (since they will be the same across all stages)
            var shaderParams = subProgInf.UsesParameterBlob
                ? blobMan.GetShaderParams((int)subProgInf.ParameterBlobIndex)
                : subProgData.ShaderParams!; //! we would have thrown if this was null by now

            if (program.CommonParams is { } commonParams)
            {
                shaderParams.CombineCommon(commonParams);
            }

            // if our program is vertex, stages may return other types like fragment
            // if this is the newer combind version. we have to expect anything to
            // return from this function.
            var stages = LoadShaderStages(subProgDataStream, version);
            foreach (var stage in stages)
            {
                // convert to usil
                var nvnUsilConverter = new NvnProgramToUsil(stage.TransCtx!); //! assigned after construction
                nvnUsilConverter.Convert();

                // get shader
                var uShaderProg = nvnUsilConverter.Shader;

                // apply metadata to ushader
                // todo: move this to a function
                var funcType = stage.Kind switch
                {
                    NvnShaderStageKind.Vertex => UShaderFunctionType.Vertex,
                    NvnShaderStageKind.Fragment => UShaderFunctionType.Fragment,
                    _ => throw new NotImplementedException("Only vertex and fragment shaders are supported at the moment")
                };
                ApplyMetadataToProgram(uShaderProg, subProgData, shaderParams, version, funcType);

                // save decompiled shader into dict
                var comboKeywords = subProgData.GlobalKeywords.Concat(subProgData.LocalKeywords)
                    .Order()
                    .ToArray();

                //! assigned after construction
                subProgs.Add(new NvnShaderSubprogram(stage.TransCtx!, shaderParams, uShaderProg, comboKeywords));
            }
        }

        return subProgs;
    }

    private static List<NvnShaderStage> LoadShaderStages(Stream data, UnityVersion version)
    {
        Span<byte> tmpBuf = stackalloc byte[8];
        data.Position = 8;
        data.Read(tmpBuf);

        var opt = new TranslationOptions(TargetLanguage.Glsl, TargetApi.OpenGL, TranslationFlags.None);

        // todo: better check needed here since this field could be non-neg 1, yet still be the newer version
        if (BinaryPrimitives.ReadInt64LittleEndian(tmpBuf) == -1)
        {
            // newer combined version (in vertex shader). split out individual shaders.
            const int MAX_STAGE_COUNT = 6;
            const int FIELD_COUNT = 4;
            const int ROW_LEN = MAX_STAGE_COUNT * sizeof(int);
            const int START_OF_SHADER_DATA = ROW_LEN * FIELD_COUNT;
            const int SWITCH_DATA_OFFSET = 0x30;

            Span<byte> mergedHeader = new byte[ROW_LEN * FIELD_COUNT];
            data.Position = 0;
            data.Read(mergedHeader);

            List<NvnShaderStage> stages = [];
            for (int i = 0; i < MAX_STAGE_COUNT; i++)
            {
                int baseOff = i * sizeof(int);

                int dataStartPos = baseOff + ROW_LEN * 1;
                int dataStart = BinaryPrimitives.ReadInt32LittleEndian(mergedHeader[dataStartPos..(dataStartPos + sizeof(int))]);
                if (dataStart == -1)
                {
                    // this stage doesn't exist
                    continue;
                }

                // todo: be consistent with sizeof or offsets
                int unk00Pos = baseOff + ROW_LEN * 0;
                uint unk00 = BinaryPrimitives.ReadUInt32LittleEndian(mergedHeader[unk00Pos..(unk00Pos + sizeof(uint))]);

                int headerLenPos = baseOff + ROW_LEN * 2;
                int headerLen = BinaryPrimitives.ReadInt32LittleEndian(mergedHeader[headerLenPos..(headerLenPos + sizeof(int))]);

                int storageFlagsPos = baseOff + ROW_LEN * 3;
                uint storageFlags = BinaryPrimitives.ReadUInt32LittleEndian(mergedHeader[storageFlagsPos..(storageFlagsPos + sizeof(uint))]);

                stages.Add(new NvnShaderStage()
                {
                    Kind = (NvnShaderStageKind)i,
                    Unk00 = unk00,
                    DataStart = dataStart,
                    HeaderLen = headerLen,
                    ShaderBodyLen = storageFlags
                });
            }

            foreach (var stage in stages)
            {
                byte[] stageBody = new byte[stage.ShaderBodyLen - SWITCH_DATA_OFFSET];

                // it's ok if we don't read everything since the rest will be 00s
                data.Position = START_OF_SHADER_DATA + stage.DataStart + stage.HeaderLen + SWITCH_DATA_OFFSET;
                data.Read(stageBody, 0, stageBody.Length);

                stage.TransCtx = Translator.CreateContext(0, new GpuAccessor(stageBody), opt);
            }

            return stages;
        }
        else
        {
            // older separated version
            const int HEADER_SIZE = 0x10;
            const int START_OF_SHADER_DATA = HEADER_SIZE;
            const int SWITCH_DATA_OFFSET = 0x30;

            Span<byte> singleHeader = new byte[HEADER_SIZE];
            data.Position = 0;
            data.Read(singleHeader);

            // todo: be consistent with sizeof or offsets
            var kind = (NvnShaderStageKind)BinaryPrimitives.ReadInt32LittleEndian(singleHeader[0..(0 + sizeof(int))]);
            uint unk00 = BinaryPrimitives.ReadUInt32LittleEndian(singleHeader[4..(4 + sizeof(uint))]);
            int headerLen = BinaryPrimitives.ReadInt32LittleEndian(singleHeader[8..(8 + sizeof(int))]);
            uint shaderBodyLen = BinaryPrimitives.ReadUInt32LittleEndian(singleHeader[12..(12 + sizeof(uint))]);

            var stage = new NvnShaderStage()
            {
                Kind = kind,
                Unk00 = unk00,
                DataStart = 0,
                HeaderLen = headerLen,
                ShaderBodyLen = shaderBodyLen
            };

            byte[] stageBody = new byte[shaderBodyLen - SWITCH_DATA_OFFSET];

            // it's ok if we don't read everything since the rest will be 00s
            data.Position = START_OF_SHADER_DATA + stage.DataStart + stage.HeaderLen + SWITCH_DATA_OFFSET;
            data.Read(stageBody, 0, stageBody.Length);

            stage.TransCtx = Translator.CreateContext(0, new GpuAccessor(stageBody), opt);

            return [stage];
        }
    }

    private static void ApplyMetadataToProgram(
        UShaderProgram uShaderProg, ShaderSubProgramData subProgram,
        ShaderParameters shaderParams, UnityVersion version,
        UShaderFunctionType shaderFuncType)
    {
        var shaderProgType = subProgram.GetProgramType(version);
        if (shaderFuncType == UShaderFunctionType.Unknown)
        {
            throw new NotSupportedException("Only vertex and fragment shaders are supported at the moment");
        }

        uShaderProg.ShaderFunctionType = shaderFuncType;

        UsilOptimizerApplier.Apply(uShaderProg, shaderParams);
    }

    private static bool IsSupportedProgram(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.ConsoleVS or
            ShaderGpuProgramType.ConsoleFS => true,
            _ => false,
        };
    }

    public class NvnShaderSubprogram
    {
        public TranslatorContext NvnShader;
        public ShaderParameters Parameters;
        public UShaderProgram UShaderProg;
        public string[] Keywords;

        public NvnShaderSubprogram(
            TranslatorContext nvnShader, ShaderParameters parameters,
            UShaderProgram uShaderProg, string[] keywords)
        {
            NvnShader = nvnShader;
            Parameters = parameters;
            UShaderProg = uShaderProg;
            Keywords = keywords;
        }
    }

    public class NvnShaderStage
    {
        public NvnShaderStageKind Kind;
        public uint Unk00;
        public int DataStart; // only on merged
        public int HeaderLen;
        public uint ShaderBodyLen;
        public TranslatorContext? TransCtx;
    }

    public enum NvnShaderStageKind
    {
        Vertex = 0,
        Fragment = 1,
        Geometry = 2,
        TessControl = 3,
        TessEvaluation = 4,
        Compute = 5
    }
}
