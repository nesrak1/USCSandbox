using System.Text;

namespace USCSandbox.Extras
{
    public class StringBuilderIndented
    {
        private readonly StringBuilder _sb;
        private readonly int _indentSize = 4;
        private string _indent = "";

        public StringBuilderIndented(int indentSize = 4)
        {
            _sb = new StringBuilder();
            _indentSize = indentSize;
        }

        public void Indent()
        {
            _indent += new string(' ', _indentSize);
        }

        public void Unindent()
        {
            if (_indent.Length >= _indentSize)
                _indent = new string(' ', _indent.Length - _indentSize);
            else
                _indent = "";
        }

        public int GetIndent()
        {
            return _indent.Length / 4;
        }

        public void Clear()
        {
            _sb.Clear();
        }

        public void Append(string str)
        {
            _sb.Append(_indent);
            _sb.Append(str);
        }

        public void AppendLine(string str)
        {
            _sb.Append(_indent);
            _sb.AppendLine(str);
        }

        public void AppendNoIndent(string str)
        {
            _sb.Append(str);
        }

        public void AppendLineNoIndent(string str)
        {
            _sb.AppendLine(str);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
