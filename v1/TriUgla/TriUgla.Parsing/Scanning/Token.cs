namespace TriUgla.Parsing.Scanning
{
    public readonly struct Token
    {
        public readonly ETokenType type;
        public readonly int line;
        public readonly int column;
        public readonly string value;

        public Token(in Token other, string value) 
            : this(other.type, other.line, other.column, value)
        {
            
        }

        public Token(ETokenType type, int line, int column) 
            : this(type, line, column, String.Empty)
        {
            
        }

        public Token(ETokenType type, int line, int column, string value)
        {
            this.type = type;
            this.line = line;
            this.column = column;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{type} @({line},{column}) {value}";
        }
    }
}
