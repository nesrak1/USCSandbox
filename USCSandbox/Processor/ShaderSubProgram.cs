using AssetRipper.Primitives;
using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class ShaderSubProgram
    {
        public int ProgramType;
        public int StatsALU;
        public int StatsTEX;
        public int StatsFlow;
        public int StatsTempRegister;

        public List<string> GlobalKeywords;
        public List<string> LocalKeywords;

        public byte[] ProgramData;
        public ParserBindChannels BindChannels;

        public ShaderParams ShaderParams;

        public ShaderSubProgram(AssetsFileReader r, UnityVersion version)
        {
            var hasStatsTempRegister = version.IsGreaterEqual(5, 5);
            var hasLocalKeywords = version.IsLess(2021, 2) && version.IsGreaterEqual(2019, 1);

            var blobVersion = r.ReadInt32();
            ProgramType = r.ReadInt32();
            StatsALU = r.ReadInt32();
            StatsTEX = r.ReadInt32();
            StatsFlow = r.ReadInt32();
            if (hasStatsTempRegister)
            {
                StatsTempRegister = r.ReadInt32();
            }

            var globalKeywordCount = r.ReadInt32();
            GlobalKeywords = new List<string>(globalKeywordCount);
            for (var i = 0; i < globalKeywordCount; i++)
            {
                GlobalKeywords.Add(r.ReadCountStringInt32());
                r.Align();
            }
            if (hasLocalKeywords)
            {
                var localKeywordCount = r.ReadInt32();
                LocalKeywords = new List<string>(localKeywordCount);
                for (var i = 0; i < localKeywordCount; i++)
                {
                    LocalKeywords.Add(r.ReadCountStringInt32());
                    r.Align();
                }
            }
            else
            {
                LocalKeywords = new List<string>(0);
            }

            var programDataSize = r.ReadInt32();
            ProgramData = r.ReadBytes(programDataSize);
            r.Align();

            BindChannels = new ParserBindChannels(r);

            if (version.IsLess(2021))
            {
                ShaderParams = new ShaderParams(r, version, false);
            }
        }

        public ShaderGpuProgramType GetProgramType(UnityVersion version)
        {
            if (version.IsGreaterEqual(5, 5))
            {
                return ((ShaderGpuProgramType55)ProgramType).ToGpuProgramType();
            }
            else
            {
                return ((ShaderGpuProgramType53)ProgramType).ToGpuProgramType();
            }
        }
    }
}