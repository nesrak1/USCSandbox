using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.DirectXDisassembler;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.DirectX;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL;
using AssetRipper.Primitives;
using AssetsTools.NET;
using Ryujinx.Graphics.Shader.Translation;
using USCSandbox;
using USCSandbox.Processor;
using USCSandbox.UltraShaderConverter.NVN;
using USCSandbox.UltraShaderConverter.UShader.NVN;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.Converter
{
    public class USCShaderConverter
    {
        public byte[] dbgData1 = new byte[0];
        public byte[] dbgData2 = new byte[0];
        public DirectXCompiledShader? DxShader { get; set; }
        public NvnUnityShader? NvnShader { get; set; }
        public UShaderProgram? ShaderProgram { get; set; }

        public void LoadDirectXCompiledShader(Stream data, GPUPlatform graphicApi, UnityVersion version)
        {
            int offset = GetDirectXDataOffset(version, graphicApi, data.ReadByte());
            var trimmedData = new SegmentStream(data, offset);
            DxShader = new DirectXCompiledShader(trimmedData);
        }

        private static int GetDirectXDataOffset(UnityVersion version, GPUPlatform graphicApi, int headerVersion)
        {
            bool hasHeader = graphicApi != GPUPlatform.d3d9;
            if (hasHeader)
            {
                bool hasGSInputPrimitive = version.IsGreaterEqual(5, 4);
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

        public void LoadUnityNvnShader(Stream data, GPUPlatform graphicApi, UnityVersion version)
        {
            byte[] fTest = new byte[8];
            data.Position = 8;
            data.Read(fTest, 0, 8);

            if (BitConverter.ToInt64(fTest) == -1)
            {
                // newer merged version
                data.Position = 0x18;
                BinaryReader br = new BinaryReader(data);

                uint shaderFragOffset = br.ReadUInt32();
                uint shaderVertOffset = br.ReadUInt32();
                data.Position += 16;
                uint shaderFragDataOffset = br.ReadUInt32();
                uint shaderVertDataOffset = br.ReadUInt32();
                data.Position += 16;
                uint shaderFragFlags = br.ReadUInt32();
                uint shaderVertFlags = br.ReadUInt32();
                data.Position += 16;

                long basePosition = data.Position;

                const int SECOND_OFFSET = 0x30;
                long shaderVertPosition = basePosition + shaderVertOffset + shaderVertDataOffset + SECOND_OFFSET;
                long shaderFragPosition = basePosition + shaderFragOffset + shaderFragDataOffset + SECOND_OFFSET;
                int shaderVertLength = (int)(shaderFragPosition - shaderVertPosition);
                int shaderFragLength = (int)(data.Length - shaderFragPosition);

                data.Position = shaderFragPosition;
                byte[] fragBytes = br.ReadBytes(shaderFragLength);
                data.Position = shaderVertPosition;
                byte[] vertBytes = br.ReadBytes(shaderVertLength);

                dbgData1 = vertBytes;
                dbgData2 = fragBytes;

                TranslationOptions opt = new TranslationOptions(TargetLanguage.Glsl, TargetApi.OpenGL, TranslationFlags.None);
                TranslatorContext fragCtx = Translator.CreateContext(0, new GpuAccessor(fragBytes), opt);
                TranslatorContext vertCtx = Translator.CreateContext(0, new GpuAccessor(vertBytes), opt);

                NvnShader = new NvnUnityShader(vertCtx, fragCtx);
            }
            else
            {
                throw new Exception("old format not supported");
                // older separated version
            }
        }

        public void ConvertDxShaderToUShaderProgram()
        {
            if (DxShader == null)
            {
                throw new Exception($"You need to call {nameof(LoadDirectXCompiledShader)} first!");
            }

            DirectXProgramToUSIL dx2UsilConverter = new DirectXProgramToUSIL(DxShader);
            dx2UsilConverter.Convert();

            ShaderProgram = dx2UsilConverter.shader;
        }

        // type is ignored if shader is not combined
        public void ConvertNvnShaderToUShaderProgram(ShaderGpuProgramType type)
        {
            if (NvnShader == null)
            {
                throw new Exception($"You need to call {nameof(LoadUnityNvnShader)} first!");
            }

            TranslatorContext? ctx = null;
            if (NvnShader.CombinedShader)
            {
                if (type == ShaderGpuProgramType.ConsoleVS)
                {
                    ctx = NvnShader.VertShader;
                }
                else if (type == ShaderGpuProgramType.ConsoleFS)
                {
                    ctx = NvnShader.FragShader;
                }
                else
                {
                    throw new NotSupportedException("Only vertex and fragment shaders are supported at the moment!");
                }
            }
            else
            {
                ctx = NvnShader.OnlyShader;
            }

            if (ctx == null)
            {
                throw new Exception("Shader type not found!");
            }

            NvnProgramToUSIL nvn2UsilConverter = new NvnProgramToUSIL(ctx);
            nvn2UsilConverter.Convert();

            ShaderProgram = nvn2UsilConverter.shader;
        }

        public void ApplyMetadataToProgram(ShaderSubProgram subProgram, ShaderParams shaderParams, UnityVersion version)
        {
            if (ShaderProgram == null)
            {
                throw new Exception($"You need to call {nameof(ConvertDxShaderToUShaderProgram)} first!");
            }

            ShaderGpuProgramType shaderProgramType = subProgram.GetProgramType(version);

            bool isVertex = shaderProgramType == ShaderGpuProgramType.DX11VertexSM40 || shaderProgramType == ShaderGpuProgramType.DX11VertexSM50 || shaderProgramType == ShaderGpuProgramType.ConsoleVS;
            bool isFragment = shaderProgramType == ShaderGpuProgramType.DX11PixelSM40 || shaderProgramType == ShaderGpuProgramType.DX11PixelSM50 || shaderProgramType == ShaderGpuProgramType.ConsoleVS || shaderProgramType == ShaderGpuProgramType.ConsoleFS;

            if (!isVertex && !isFragment)
            {
                throw new NotSupportedException("Only vertex and fragment shaders are supported at the moment!");
            }

            ShaderProgram.shaderFunctionType = isVertex ? UShaderFunctionType.Vertex : UShaderFunctionType.Fragment;

            USILOptimizerApplier.Apply(ShaderProgram, shaderParams);
        }
    }
}
