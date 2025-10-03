namespace TriUgla.Parsing.Scanning
{
    public readonly struct Token
    {
        public readonly ETokenType type;
        public readonly int line;
        public readonly int column;
        public readonly string value;
        public readonly double numeric;

        public Token(ETokenType type, int line, int column, string value, double numeric = Double.NaN)
        {
            this.type = type;
            this.line = line;
            this.column = column;
            this.value = value;
            this.numeric = numeric;
        }

        public override string ToString()
        {
            return $"{type} @({line},{column}) {value}";
        }
    }
}
