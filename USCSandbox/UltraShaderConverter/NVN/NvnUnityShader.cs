using Ryujinx.Graphics.Shader.Translation;

namespace USCSandbox.UltraShaderConverter.NVN
{
    public class NvnUnityShader
    {
        public NvnUnityShader(TranslatorContext vertShader)
        {
            CombinedShader = false;
            VertShader = vertShader;
        }

        public NvnUnityShader(TranslatorContext vertShader, TranslatorContext fragShader)
        {
            CombinedShader = true;
            VertShader = vertShader;
            FragShader = fragShader;
        }

        public bool CombinedShader { get; private set; }
        public TranslatorContext? VertShader { get; private set; }
        public TranslatorContext? FragShader { get; private set; }
        public TranslatorContext? OnlyShader => VertShader;
    }
}
