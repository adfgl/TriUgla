namespace TriUgla.Parsing.Scanning
{
    public static class Keywords
    {
        public static IReadOnlyDictionary<string, ETokenType> Source => _keywords;

        static readonly Dictionary<string, ETokenType> _keywords = new Dictionary<string, ETokenType>()
        {
            { "Print", ETokenType.Print },

            { "Point", ETokenType.Point },
            { "Line", ETokenType.Line },

            { "If", ETokenType.If },
            { "Else", ETokenType.Else },
            { "ElseIf", ETokenType.ElseIf },
            { "EndIf", ETokenType.EndIf },

            { "For", ETokenType.For },
            { "In", ETokenType.In },
            { "EndFor", ETokenType.EndFor },

            { "Macro", ETokenType.Macro },
            { "EndMacro", ETokenType.EndMacro },
            { "Call", ETokenType.Call },

            { "and", ETokenType.And },
            { "or", ETokenType.Or },
            { "not", ETokenType.Not },
            { "is", ETokenType.EqualEqual }
        };
    }
}
