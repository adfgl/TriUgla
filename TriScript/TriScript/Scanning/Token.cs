namespace TriScript.Scanning
{
    public readonly struct Token
    {
        public readonly ETokenType type;
        public readonly int line, column;
        public readonly TextSpan span;

        public Token(ETokenType type, int line, int column, TextSpan span)
        {
            this.type = type;
            this.line = line;
            this.column = column;
            this.span = span;
        }

        public override string ToString()
        {
            return $"({line}:{column}) {type}";
        }
    }
}
