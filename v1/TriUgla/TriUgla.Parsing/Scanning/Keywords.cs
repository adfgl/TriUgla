namespace TriUgla.Parsing.Scanning
{
    public static class Keywords
    {
        public static IReadOnlyDictionary<string, ETokenType> Source => _keywords;

        static readonly Dictionary<string, ETokenType> _keywords = new Dictionary<string, ETokenType>()
        {
            { "Point", ETokenType.Point },
            { "Line", ETokenType.Line },
            { "If", ETokenType.If },
            { "Else", ETokenType.Else },
            { "ElseIf", ETokenType.ElseIf },
            { "EndIf", ETokenType.EndIf },
        };
    }
}
