using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class ParserBindChannels
    {
        public int SourceMap;
        public ShaderBindChannel[] Channels;

        public ParserBindChannels(AssetsFileReader r)
        {
            SourceMap = r.ReadInt32();
            var bindCount = r.ReadInt32();
            Channels = new ShaderBindChannel[bindCount];
            for (int i = 0; i < bindCount; i++)
            {
                var channel = new ShaderBindChannel(r);
                Channels[i] = channel;
                SourceMap |= 1 << channel.Source;
            }
        }
    }
}