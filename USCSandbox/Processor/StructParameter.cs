using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class StructParameter
    {
        public string Name;
        public int Index;
        public int ArraySize;
        public int Size;
        public List<ConstantBufferParameter> CBParams;

        public StructParameter(AssetsFileReader r)
        {
            Name = r.ReadCountStringInt32();
            Index = r.ReadInt32();
            ArraySize = r.ReadInt32();
            Size = r.ReadInt32();

            var paramCount = r.ReadInt32();
            CBParams = new List<ConstantBufferParameter>(paramCount);
            for (var j = 0; j < paramCount; j++)
            {
                CBParams.Add(new ConstantBufferParameter(r, Name));
            }
        }
    }
}