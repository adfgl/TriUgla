namespace TriScript.Scanning
{
    public readonly struct TextPosition
    {
        public readonly int line, column;

        public TextPosition(int line, int column)
        {
            this.line = line;
            this.column = column;
        }

        public override string ToString()
        {
            return $"Ln: {line + 1} Ch: {column + 1}";
        }
    }
}
