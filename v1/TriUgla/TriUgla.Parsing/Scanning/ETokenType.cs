
namespace TriUgla.Parsing.Scanning
{
    public enum ETokenType
    {
        Undefined, 
        Break, Continue,

        NameOf, Exists, Get, Set,

        Print, Abort, Return, NativeFunction,
        Error,
        Comment, MultiLineComment, 
        LineBreak, EOF,

        Numeric, String, Identifier,

        Minus, Plus, Star, Slash, Modulo, Power, 
        MinusEqual, PlusEqual, StarEqual, SlashEqual, ModuloEqual, PowerEqual,
        PlusPlus, MinusMinus,

        OpenParen, CloseParen,
        OpenCurly, CloseCurly,
        OpenSquare, CloseSquare,

        Equal, Tilda,

        Colon, SemiColon, Comma, Dot, Hash,

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
