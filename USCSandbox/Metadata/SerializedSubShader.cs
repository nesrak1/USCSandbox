using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedSubShader
{
    public List<SerializedPass> Passes;
    public Dictionary<string, string> Tags;
    public int LOD;

    public SerializedSubShader(AssetTypeValueField field)
    {
        Passes = field["m_Passes.Array"]
            .Select(p => new SerializedPass(p)).ToList();

        Tags = field["m_Tags.tags.Array"]
            .ToDictionary(tp => tp[0].AsString, tp => tp[1].AsString);

        LOD = field["m_LOD"].AsInt;
    }
}
