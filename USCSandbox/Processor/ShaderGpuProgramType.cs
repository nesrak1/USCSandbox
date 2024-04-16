namespace USCSandbox.Processor
{
    public enum ShaderGpuProgramType
    {
        Unknown = 0,
        GLLegacy = 1,
        GLES31AEP = 2,
        GLES31 = 3,
        GLES3 = 4,
        GLES = 5,
        GLCore32 = 6,
        GLCore41 = 7,
        GLCore43 = 8,
        DX9VertexSM20 = 9,
        DX9VertexSM30 = 10,
        DX9PixelSM20 = 11,
        DX9PixelSM30 = 12,
        DX10Level9Vertex = 13,
        DX10Level9Pixel = 14,
        DX11VertexSM40 = 15,
        DX11VertexSM50 = 16,
        DX11PixelSM40 = 17,
        DX11PixelSM50 = 18,
        DX11GeometrySM40 = 19,
        DX11GeometrySM50 = 20,
        DX11HullSM50 = 21,
        DX11DomainSM50 = 22,
        MetalVS = 23,
        MetalFS = 24,
        SPIRV = 25,
        Console = 26,
        ConsoleVS = 26,
        ConsoleFS = 27,
        ConsoleHS = 28,
        ConsoleDS = 29,
        ConsoleGS = 30,
        RayTracing = 31,
        PS5NGGC = 32
    }

    public enum ShaderGpuProgramType55
    {
        Unknown = 0,
        GLLegacy = 1,
        GLES31AEP = 2,
        GLES31 = 3,
        GLES3 = 4,
        GLES = 5,
        GLCore32 = 6,
        GLCore41 = 7,
        GLCore43 = 8,
        DX9VertexSM20 = 9,
        DX9VertexSM30 = 10,
        DX9PixelSM20 = 11,
        DX9PixelSM30 = 12,
        DX10Level9Vertex = 13,
        DX10Level9Pixel = 14,
        DX11VertexSM40 = 15,
        DX11VertexSM50 = 16,
        DX11PixelSM40 = 17,
        DX11PixelSM50 = 18,
        DX11GeometrySM40 = 19,
        DX11GeometrySM50 = 20,
        DX11HullSM50 = 21,
        DX11DomainSM50 = 22,
        MetalVS = 23,
        MetalFS = 24,
        SPIRV = 25,
        Console = 26,
        //ConsoleVS = 26,
        ConsoleFS = 27,
        ConsoleHS = 28,
        ConsoleDS = 29,
        ConsoleGS = 30,
        RayTracing = 31,
    }

    public enum ShaderGpuProgramType53
    {
        Unknown = 0,
        GLLegacy = 1,
        GLES31AEP = 2,
        GLES31 = 3,
        GLES3 = 4,
        GLES = 5,
        GLCore32 = 6,
        GLCore41 = 7,
        GLCore43 = 8,
        DX9VertexSM20 = 9,
        DX9VertexSM30 = 10,
        DX9PixelSM20 = 11,
        DX9PixelSM30 = 12,
        DX10Level9Vertex = 13,
        DX10Level9Pixel = 14,
        DX11VertexSM40 = 15,
        DX11VertexSM50 = 16,
        DX11PixelSM40 = 17,
        DX11PixelSM50 = 18,
        DX11GeometrySM40 = 19,
        DX11GeometrySM50 = 20,
        DX11HullSM50 = 21,
        DX11DomainSM50 = 22,
        MetalVS = 23,
        MetalFS = 24,
        ConsoleVS = 25,
        ConsoleFS = 26,
        ConsoleHS = 27,
        ConsoleDS = 28,
        ConsoleGS = 29,
    }

    public static class ShaderGpuProgramTypeExtensions
    {
        public static ShaderGpuProgramType ToGpuProgramType(this ShaderGpuProgramType55 _this)
        {
            return _this switch
            {
                ShaderGpuProgramType55.Unknown => ShaderGpuProgramType.Unknown,
                ShaderGpuProgramType55.GLLegacy => ShaderGpuProgramType.GLLegacy,
                ShaderGpuProgramType55.GLES31AEP => ShaderGpuProgramType.GLES31AEP,
                ShaderGpuProgramType55.GLES31 => ShaderGpuProgramType.GLES31,
                ShaderGpuProgramType55.GLES3 => ShaderGpuProgramType.GLES3,
                ShaderGpuProgramType55.GLES => ShaderGpuProgramType.GLES,
                ShaderGpuProgramType55.GLCore32 => ShaderGpuProgramType.GLCore32,
                ShaderGpuProgramType55.GLCore41 => ShaderGpuProgramType.GLCore41,
                ShaderGpuProgramType55.GLCore43 => ShaderGpuProgramType.GLCore43,
                ShaderGpuProgramType55.DX9VertexSM20 => ShaderGpuProgramType.DX9VertexSM20,
                ShaderGpuProgramType55.DX9VertexSM30 => ShaderGpuProgramType.DX9VertexSM30,
                ShaderGpuProgramType55.DX9PixelSM20 => ShaderGpuProgramType.DX9PixelSM20,
                ShaderGpuProgramType55.DX9PixelSM30 => ShaderGpuProgramType.DX9PixelSM30,
                ShaderGpuProgramType55.DX10Level9Vertex => ShaderGpuProgramType.DX10Level9Vertex,
                ShaderGpuProgramType55.DX10Level9Pixel => ShaderGpuProgramType.DX10Level9Pixel,
                ShaderGpuProgramType55.DX11VertexSM40 => ShaderGpuProgramType.DX11VertexSM40,
                ShaderGpuProgramType55.DX11VertexSM50 => ShaderGpuProgramType.DX11VertexSM50,
                ShaderGpuProgramType55.DX11PixelSM40 => ShaderGpuProgramType.DX11PixelSM40,
                ShaderGpuProgramType55.DX11PixelSM50 => ShaderGpuProgramType.DX11PixelSM50,
                ShaderGpuProgramType55.DX11GeometrySM40 => ShaderGpuProgramType.DX11GeometrySM40,
                ShaderGpuProgramType55.DX11GeometrySM50 => ShaderGpuProgramType.DX11GeometrySM50,
                ShaderGpuProgramType55.DX11HullSM50 => ShaderGpuProgramType.DX11HullSM50,
                ShaderGpuProgramType55.DX11DomainSM50 => ShaderGpuProgramType.DX11DomainSM50,
                ShaderGpuProgramType55.MetalVS => ShaderGpuProgramType.MetalVS,
                ShaderGpuProgramType55.MetalFS => ShaderGpuProgramType.MetalFS,
                ShaderGpuProgramType55.SPIRV => ShaderGpuProgramType.SPIRV,
                ShaderGpuProgramType55.Console => ShaderGpuProgramType.Console,
                ShaderGpuProgramType55.ConsoleFS => ShaderGpuProgramType.Console,
                ShaderGpuProgramType55.ConsoleHS => ShaderGpuProgramType.Console,
                ShaderGpuProgramType55.ConsoleDS => ShaderGpuProgramType.Console,
                ShaderGpuProgramType55.ConsoleGS => ShaderGpuProgramType.Console,
                ShaderGpuProgramType55.RayTracing => ShaderGpuProgramType.RayTracing,
                _ => throw new Exception($"Unsupported gpu program type {_this}"),
            };
        }

        public static ShaderGpuProgramType ToGpuProgramType(this ShaderGpuProgramType53 _this)
        {
            return _this switch
            {
                ShaderGpuProgramType53.Unknown => ShaderGpuProgramType.Unknown,
                ShaderGpuProgramType53.GLLegacy => ShaderGpuProgramType.GLLegacy,
                ShaderGpuProgramType53.GLES31AEP => ShaderGpuProgramType.GLES31AEP,
                ShaderGpuProgramType53.GLES31 => ShaderGpuProgramType.GLES31,
                ShaderGpuProgramType53.GLES3 => ShaderGpuProgramType.GLES3,
                ShaderGpuProgramType53.GLES => ShaderGpuProgramType.GLES,
                ShaderGpuProgramType53.GLCore32 => ShaderGpuProgramType.GLCore32,
                ShaderGpuProgramType53.GLCore41 => ShaderGpuProgramType.GLCore41,
                ShaderGpuProgramType53.GLCore43 => ShaderGpuProgramType.GLCore43,
                ShaderGpuProgramType53.DX9VertexSM20 => ShaderGpuProgramType.DX9VertexSM20,
                ShaderGpuProgramType53.DX9VertexSM30 => ShaderGpuProgramType.DX9VertexSM30,
                ShaderGpuProgramType53.DX9PixelSM20 => ShaderGpuProgramType.DX9PixelSM20,
                ShaderGpuProgramType53.DX9PixelSM30 => ShaderGpuProgramType.DX9PixelSM30,
                ShaderGpuProgramType53.DX10Level9Vertex => ShaderGpuProgramType.DX10Level9Vertex,
                ShaderGpuProgramType53.DX10Level9Pixel => ShaderGpuProgramType.DX10Level9Pixel,
                ShaderGpuProgramType53.DX11VertexSM40 => ShaderGpuProgramType.DX11VertexSM40,
                ShaderGpuProgramType53.DX11VertexSM50 => ShaderGpuProgramType.DX11VertexSM50,
                ShaderGpuProgramType53.DX11PixelSM40 => ShaderGpuProgramType.DX11PixelSM40,
                ShaderGpuProgramType53.DX11PixelSM50 => ShaderGpuProgramType.DX11PixelSM50,
                ShaderGpuProgramType53.DX11GeometrySM40 => ShaderGpuProgramType.DX11GeometrySM40,
                ShaderGpuProgramType53.DX11GeometrySM50 => ShaderGpuProgramType.DX11GeometrySM50,
                ShaderGpuProgramType53.DX11HullSM50 => ShaderGpuProgramType.DX11HullSM50,
                ShaderGpuProgramType53.DX11DomainSM50 => ShaderGpuProgramType.DX11DomainSM50,
                ShaderGpuProgramType53.MetalVS => ShaderGpuProgramType.MetalVS,
                ShaderGpuProgramType53.MetalFS => ShaderGpuProgramType.MetalFS,
                ShaderGpuProgramType53.ConsoleVS => ShaderGpuProgramType.ConsoleVS,
                ShaderGpuProgramType53.ConsoleFS => ShaderGpuProgramType.ConsoleFS,
                ShaderGpuProgramType53.ConsoleHS => ShaderGpuProgramType.ConsoleHS,
                ShaderGpuProgramType53.ConsoleDS => ShaderGpuProgramType.ConsoleDS,
                ShaderGpuProgramType53.ConsoleGS => ShaderGpuProgramType.ConsoleGS,
                _ => throw new Exception($"Unsupported gpu program type {_this}"),
            };
        }

        public static GPUPlatform ToGPUPlatform(this ShaderGpuProgramType _this)
        {
            switch (_this)
            {
                case ShaderGpuProgramType.Unknown:
                    return GPUPlatform.unknown;

                case ShaderGpuProgramType.GLES:
                    return GPUPlatform.gles;

                case ShaderGpuProgramType.GLES3:
                case ShaderGpuProgramType.GLES31:
                case ShaderGpuProgramType.GLES31AEP:
                    return GPUPlatform.gles3;

                case ShaderGpuProgramType.GLCore32:
                case ShaderGpuProgramType.GLCore41:
                case ShaderGpuProgramType.GLCore43:
                    return GPUPlatform.glcore;

                case ShaderGpuProgramType.GLLegacy:
                    return GPUPlatform.openGL;

                case ShaderGpuProgramType.DX9VertexSM20:
                case ShaderGpuProgramType.DX9VertexSM30:
                case ShaderGpuProgramType.DX9PixelSM20:
                case ShaderGpuProgramType.DX9PixelSM30:
                    return GPUPlatform.d3d9;

                case ShaderGpuProgramType.DX10Level9Pixel:
                case ShaderGpuProgramType.DX10Level9Vertex:
                    return GPUPlatform.d3d11_9x;

                case ShaderGpuProgramType.DX11VertexSM40:
                case ShaderGpuProgramType.DX11VertexSM50:
                case ShaderGpuProgramType.DX11PixelSM40:
                case ShaderGpuProgramType.DX11PixelSM50:
                case ShaderGpuProgramType.DX11GeometrySM40:
                case ShaderGpuProgramType.DX11GeometrySM50:
                case ShaderGpuProgramType.DX11HullSM50:
                case ShaderGpuProgramType.DX11DomainSM50:
                    return GPUPlatform.d3d11;

                case ShaderGpuProgramType.MetalVS:
                case ShaderGpuProgramType.MetalFS:
                    return GPUPlatform.metal;

                case ShaderGpuProgramType.SPIRV:
                    return GPUPlatform.vulkan;

                case ShaderGpuProgramType.ConsoleVS:
                case ShaderGpuProgramType.ConsoleFS:
                case ShaderGpuProgramType.ConsoleHS:
                case ShaderGpuProgramType.ConsoleDS:
                case ShaderGpuProgramType.ConsoleGS:
                    throw new NotSupportedException($"Console not supported right now");

                default:
                    throw new NotSupportedException($"Unsupported gpu program type {_this}");
            }
        }
    }
}
