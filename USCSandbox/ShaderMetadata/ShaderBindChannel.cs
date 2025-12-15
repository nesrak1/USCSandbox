using AssetsTools.NET;

namespace USCSandbox.ShaderMetadata;
public class ShaderBindChannel
{
    public int Source;
    public VertexComponent Target;

    public ShaderBindChannel(AssetsFileReader r)
    {
        Source = r.ReadInt32();
        Target = (VertexComponent)r.ReadInt32();
    }
}