namespace TriScript.Scanning
{
    public readonly struct Token
    {
        public readonly ETokenType type;
        public readonly TextPosition position;
        public readonly TextSpan span;

        public Token(ETokenType type, TextPosition position, TextSpan span)
        {
            this.type = type;
            this.position = position;
            this.span = span;
        }

        public ReadOnlySpan<char> GetSpan(Source src) => src.Slice(span);
        public string GetString(Source src) => src.GetString(span);

        public override string ToString()
        {
            return $"{position} {type} [{span}]";
        }
    }
}
