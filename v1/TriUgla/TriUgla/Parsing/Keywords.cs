using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing
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
