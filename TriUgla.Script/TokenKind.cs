namespace TriUgla.Script
{
    public enum TokenKind
    {
        Undefined,
        EndOfFile,
        LineBreak,

        Keyword,
        Identifier,
        Number,
        String,

        OpenParen,
        CloseParen,
        OpenCurly,
        CloseCurly,
        OpenSquare,
        CloseSquare,

        Comma,
        Colon,
        Semicolon,

        Operator
    }
}
