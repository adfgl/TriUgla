namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,

        LineBreak, 
        EndOfFile,

        LiteralNemeric, 
        LiteralString,
        LiteralId,

        Plus, 
        Minus,
        Star,
        Slash,

        Not,
        Or,
        And,

        Bar,
        Amp,

        PlusPlus,
        MinusMinus,

        Colon, SemiColon, Dot, Comma,

        Assign,

        Equal,
        NotEqual,

        Greater, Less,
        GreaterEqual, LessEqaul,

        OpenParen, CloseParen,
        OpenCurly, CloseCurly,
        OpenSquare, CloseSquare,
    }
}
