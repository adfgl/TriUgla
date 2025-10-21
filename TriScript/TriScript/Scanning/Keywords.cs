namespace TriScript.Scanning
{
    public static class Keywords
    {
        readonly static Dictionary<string, ETokenType> _source = new Dictionary<string, ETokenType>()
        {
            { "print", ETokenType.Print },

            { "true", ETokenType.True },
            { "false" , ETokenType.False },

            { "for", ETokenType.For },
            { "in", ETokenType.In },

            { "if" , ETokenType.If },
            { "else" , ETokenType.Else },

            { "dot", ETokenType.DotProduct },
            { "cross", ETokenType.CrossProduct },

            { "break" , ETokenType.Break },
            { "continue" , ETokenType.Continue },

        };

        public static IReadOnlyDictionary<string, ETokenType> Source => _source;
    }
}
