using AssetsTools.NET;

namespace USCSandbox.Metadata;
public static class SerializedMetadataHelpers
{
    public static AssetTypeValueField GetArrayFirstValue(AssetTypeValueField field)
    {
        // access field["Array"][^1] if it exists (new version)
        // else access field normally. not sure why there might be
        // more than one used element in the new version array...
        var tempField = field.TemplateField;
        if (tempField.Children.Count < 2)
            return field;

        var possibleArrayType = tempField[1];
        if (possibleArrayType.Type != "vector" || possibleArrayType.Name != "data")
            return field;

        return field[field.Children.Count - 1]["Array"];
    }
}
