using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class UAVParameter
    {
        public string Name;
        public int Index;
        public int OriginalIndex;

        public UAVParameter(AssetsFileReader r, string name)
        {
            Name = name;
            Index = r.ReadInt32();
            OriginalIndex = r.ReadInt32();
        }
    }
}