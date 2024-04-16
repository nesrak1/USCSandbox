using AssetRipper.Primitives;

namespace USCSandbox.Processor
{
    public class BlobEntry
    {
        public int Offset;
        public int Length;
        public int Segment;

        public BlobEntry(BinaryReader reader, UnityVersion engVer)
        {
            Offset = reader.ReadInt32();
            Length = reader.ReadInt32();
            if (engVer.IsGreaterEqual(2019, 3))
            {
                Segment = reader.ReadInt32();
            }
        }
    }
}