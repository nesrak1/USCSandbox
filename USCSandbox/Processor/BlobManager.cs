using AssetRipper.Primitives;
using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class BlobManager
    {
        private AssetsFileReader _reader;
        private UnityVersion _engVer;

        public List<BlobEntry> Entries;

        public BlobManager(byte[] blob, UnityVersion engVer)
        {
            _reader = new AssetsFileReader(new MemoryStream(blob));
            _engVer = engVer;

            var count = _reader.ReadInt32();
            Entries = new List<BlobEntry>(count);
            for (var i = 0; i < count; i++)
            {
                Entries.Add(new BlobEntry(_reader, engVer));
            }
        }

        public byte[] GetRawEntry(int index)
        {
            _reader.BaseStream.Position = Entries[index].Offset;
            return _reader.ReadBytes(Entries[index].Length);
        }

        public ShaderParams GetShaderParams(int index)
        {
            var blobEntry = GetRawEntry(index);
            var r = new AssetsFileReader(new MemoryStream(blobEntry));
            return new ShaderParams(r, _engVer, true);
        }

        public ShaderSubProgram GetShaderSubProgram(int index)
        {
            var blobEntry = GetRawEntry(index);
            var r = new AssetsFileReader(new MemoryStream(blobEntry));
            return new ShaderSubProgram(r, _engVer);
        }
    }
}