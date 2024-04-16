using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class SamplerParameter
    {
        public uint Sampler;
        public int BindPoint;

        public SamplerParameter(AssetsFileReader r)
        {
            BindPoint = r.ReadInt32();
            Sampler = r.ReadUInt32();
        }
    }
}