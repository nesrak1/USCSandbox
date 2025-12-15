using AssetsTools.NET;
using USCSandbox.Common;

namespace USCSandbox.Metadata;
public class SerializedPass
{
    public string UseName;
    public string Name;
    public List<GPUPlatform> Platforms;
    public SerializedShaderState State;
    public List<SerializedProgram> Programs;
    public Dictionary<string, string> Tags;

    public readonly string[] SUPPORTED_PROG_TYPES = [
        "progVertex", "progFragment"
    ];

    public SerializedPass(AssetTypeValueField data)
    {
        UseName = data["m_UseName"].AsString;
        Name = data["m_Name"].AsString;

        Platforms = data["m_Platforms.Array"]
            .Select(p => (GPUPlatform)p.AsByte).ToList();

        State = new SerializedShaderState(data["m_State"]);

        var nameTable = data["m_NameIndices.Array"]
            .ToDictionary(ni => ni[1].AsInt, ni => ni[0].AsString);

        Programs = SUPPORTED_PROG_TYPES
            .Select(t => new SerializedProgram(data[t], t, nameTable)).ToList();

        Tags = data["m_Tags.tags.Array"]
            .ToDictionary(ni => ni[0].AsString, ni => ni[1].AsString);
    }
}