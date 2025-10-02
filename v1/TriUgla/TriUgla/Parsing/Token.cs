using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing
{
    public readonly struct Token
    {
        public readonly ETokenType type;
        public readonly int line;
        public readonly int column;
        public readonly int length;
        public readonly Value value;

        public Token(ETokenType type, int line, int column, int length, Value value)
        {
            this.type = type;
            this.line = line;
            this.column = column;
            this.length = length;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{type} @({line},{column}) len={length} {FormatValue()}";
        }

        string FormatValue()
        {
            if (value.type == EDataType.None) return string.Empty;
            return value.ToString();
        }
    }
}
