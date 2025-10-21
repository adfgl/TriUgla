namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,

        True, False,

        For, In,
        If, Else, 

        Break, Continue,

        Print,

        DotProduct, CrossProduct,

        LineBreak, 
        EndOfFile,

        LiteralNemeric, 
        LiteralString,
        LiteralSymbol,
        LiteralIdentifier,

        Plus, 
        Minus,
        Star,
        Slash,

        Is,
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
