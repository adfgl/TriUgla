using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Scanning
{
    public enum ETokenType
    {
        Undefined, Error, Comment, MultiLineComment, LineBreak, EOF,
        NumericLiteral, StringLiteral, IdentifierLiteral,

        Minus, Plus, Star, Slash, Modulo, Power,
        MinusEqual, PlusEqual, StarEqual, SlashEqual, ModuloEqual,
        PlusPlus, MinusMinus,

        OpenParen, CloseParen,
        OpenCurly, CloseCurly,
        OpenSquare, CloseSquare,

        Equal,

        Colon, SemiColon, Comma, Dot, Hash,

        Point, Line,

        And, Or, Not, Question,
        EqualEqual, NotEqual,

        Less, LessOrEqual,
        Greater, GreaterOrEqual,

        Ampersand, Bar,

        If, Else, ElseIf, EndIf,

        For, In, EndFor,

        Macro, EndMacro, Call,
    }
}
