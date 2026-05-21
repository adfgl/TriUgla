namespace TriUgla.Script.Scanning
{
    public readonly record struct Position(int Line, int Column)
    {
        public override string ToString()
            => $"Ln: {Line + 1} Ch: {Column + 1}";
    }
}
