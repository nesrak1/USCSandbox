using AssetsTools.NET;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.Processor;

public class BlobManager
{
    private AssetsFileReader[] _readers;
    private UnityVersion _engVer;

    public List<BlobEntry> Entries;

    public BlobManager(byte[][] blobs, UnityVersion engVer)
    {
        _readers = new AssetsFileReader[blobs.Length];
        for (var i = 0; i < blobs.Length; i++)
        {
            _readers[i] = new AssetsFileReader(new MemoryStream(blobs[i]));
        }

        _engVer = engVer;

        var tableReader = _readers[0];
        var count = tableReader.ReadInt32();
        Entries = new List<BlobEntry>(count);
        for (var i = 0; i < count; i++)
        {
            Entries.Add(new BlobEntry(tableReader, engVer));
        }
    }

    public byte[] GetRawEntry(int index)
    {
        var entry = Entries[index];
        AssetsFileReader reader = entry.Segment == -1
            ? _readers[0]
            : _readers[entry.Segment];

        reader.BaseStream.Position = entry.Offset;
        return reader.ReadBytes(entry.Length);
    }

    public ShaderParameters GetShaderParams(int index)
    {
        var blobEntry = GetRawEntry(index);
        var r = new AssetsFileReader(new MemoryStream(blobEntry));
        return new ShaderParameters(r, _engVer, true);
    }

    public ShaderSubProgramData GetShaderSubProgram(int index)
    {
        var blobEntry = GetRawEntry(index);
        var r = new AssetsFileReader(new MemoryStream(blobEntry));
        return new ShaderSubProgramData(r, _engVer);
    }
}