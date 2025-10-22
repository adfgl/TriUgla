namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,

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
        Pow,

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
