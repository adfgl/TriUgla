namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,
        EndOfFile,
        LineBreak,

        LiteralNemeric,
        LiteralString,
        LiteralSymbol,

        Plus, Minus, Star, Slash, Caret,

        Less, LessEqual,
        Greater, GreaterEqual,
        Equal, NotEqual,

        OpenParen, CloseParen,
        OpenCurly, CloseCurly,
        OpenSquare, CloseSquare,

        Assign,
        Is,
        Not,
        Or,
        And,

        Bar,
        Amp,

        PlusPlus,
        MinusMinus,

        Colon, SemiColon, Dot, Comma,
    }
}
