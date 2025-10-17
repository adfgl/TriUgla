using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public static class Keywords
    {
        public static IReadOnlyDictionary<string, ETokenType> Source => _keywords;

        static readonly Dictionary<string, ETokenType> _keywords = new Dictionary<string, ETokenType>()
        {
            { "print", ETokenType.Print },
            { "abort", ETokenType.Abort },
            { "return", ETokenType.Return },
            { "test", ETokenType.Test },

            { "matrix", ETokenType.Matrix },
            { "vector", ETokenType.Vector },

            { "integer", ETokenType.Integer },
            { "float", ETokenType.Float },
            { "string", ETokenType.String },
            { "list", ETokenType.List },

            { "nameof", ETokenType.NameOf },
            { "exists", ETokenType.Exists },
            { "get", ETokenType.Get },
            { "set", ETokenType.Set },

            { "if", ETokenType.If },
            { "else", ETokenType.Else },
            { "elif", ETokenType.ElseIf },
            { "endif", ETokenType.EndIf },

            { "for", ETokenType.For },
            { "in", ETokenType.In },
            { "endfor", ETokenType.EndFor },

            { "macro", ETokenType.Macro },
            { "endmacro", ETokenType.EndMacro },
            { "call", ETokenType.Call },

            { "and", ETokenType.And },
            { "or", ETokenType.Or },
            { "not", ETokenType.Not },
            { "is", ETokenType.EqualEqual },

            { "break", ETokenType.Break },
            { "continue", ETokenType.Continue },
        };
    }
}
