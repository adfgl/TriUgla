using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public static class Keywords
    {
        public static IReadOnlyDictionary<string, ETokenType> Source => _keywords;

        static readonly Dictionary<string, ETokenType> _keywords = new Dictionary<string, ETokenType>()
        {
            { "Print", ETokenType.Print },
            { "Abort", ETokenType.Abort },
            { "Return", ETokenType.Return },

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
            { "is", ETokenType.EqualEqual },

            { "break", ETokenType.Break },
            { "continue", ETokenType.Continue },
        };
    }
}
