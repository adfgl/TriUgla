namespace TriUgla.Script.Scanning
{
    public readonly record struct Span(int Start, int Length)
    {
        public int End => Start + Length;
        public override string ToString() 
            => $"{Start + 1}..{End + 1}";
    }
}
