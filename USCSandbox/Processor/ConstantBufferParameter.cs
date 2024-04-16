using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class ConstantBufferParameter
    {
        public string ParamName;
        public ShaderParamType ParamType;
        public int Rows;
        //public int MatrixColumns;
        //public int Columns => IsMatrix ? MatrixColumns : 1;
        public int Columns;
        public bool IsMatrix;
        public int ArraySize;
        public int Index;

        public ConstantBufferParameter(AssetsFileReader r, string structName = "")
        {
            if (structName != "")
                ParamName = $"{structName}.{r.ReadCountStringInt32()}";
            else
                ParamName = r.ReadCountStringInt32();

            r.Align();

            ParamType = (ShaderParamType)r.ReadInt32();
            Rows = r.ReadInt32();
            Columns = r.ReadInt32();
            //MatrixColumns = r.ReadInt32();
            IsMatrix = r.ReadInt32() > 0;
            ArraySize = r.ReadInt32();
            Index = r.ReadInt32();
        }

        public ConstantBufferParameter(AssetTypeValueField field, Dictionary<int, string> nameTable)
        {
            ParamName = nameTable[field["m_NameIndex"].AsInt];
            Index = field["m_Index"].AsInt;
            ArraySize = field["m_ArraySize"].AsInt;
            if (!field["m_RowCount"].IsDummy)
            {
                Rows = field["m_RowCount"].AsInt;
                Columns = Rows;
                //MatrixColumns = Rows; // is this right?
                IsMatrix = true;
            }
            else
            {
                Rows = field["m_Dim"].AsInt;
                Columns = 1;
                //MatrixColumns = Rows;
                IsMatrix = false;
            }
        }
    }
}