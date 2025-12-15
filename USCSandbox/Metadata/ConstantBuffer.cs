using AssetsTools.NET;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.Metadata;
public class ConstantBuffer
{
    public string Name;
    public int UsedSize;
    public bool Partial;

    public List<ConstantBufferParameter> CBParams;
    public List<StructParameter> StructParams;

    public ConstantBuffer(AssetsFileReader r, UnityVersion engVer)
    {
        Name = r.ReadCountStringInt32();
        r.Align();

        UsedSize = r.ReadInt32();
        Partial = false;

        var paramCount = r.ReadInt32();
        CBParams = new List<ConstantBufferParameter>(paramCount);
        for (var i = 0; i < paramCount; i++)
        {
            CBParams.Add(new ConstantBufferParameter(r));
        }

        bool hasStructParams = engVer.GreaterThanOrEquals(2017, 3);
        if (hasStructParams)
        {
            var structCount = r.ReadInt32();
            StructParams = new List<StructParameter>(structCount);
            for (var i = 0; i < structCount; i++)
            {
                StructParams.Add(new StructParameter(r));
            }
        }
        else
        {
            StructParams = [];
        }
    }

    public ConstantBuffer(AssetTypeValueField field, Dictionary<int, string> nameTable)
    {
        Name = nameTable[field["m_NameIndex"].AsInt];
        UsedSize = field["m_Size"].AsInt;
        Partial = field["m_IsPartialCB"].AsBool;
        CBParams = [];

        var matrixParams = field["m_MatrixParams.Array"];
        foreach (var matrixParamField in matrixParams)
        {
            CBParams.Add(new ConstantBufferParameter(matrixParamField, nameTable));
        }

        var vectorParams = field["m_VectorParams.Array"];
        foreach (var vectorParamField in vectorParams)
        {
            CBParams.Add(new ConstantBufferParameter(vectorParamField, nameTable));
        }

        var structParams = field["m_StructParams.Array"];
        if (structParams.AsArray.size > 0)
        {
            throw new NotSupportedException();
        }
        else
        {
            StructParams = [];
        }
    }
}
