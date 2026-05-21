namespace TriUgla.Script.Scanning
{
    public enum TokenKind
    {
        Error,
        Undefined,

        EndOfFile,
        LineBreak,

        Comment,
        MultiLineComment,

        Identifier,
        Keyword,
        Number,
        String,

        OpenParen,
        CloseParen,
        OpenCurly,
        CloseCurly,
        OpenSquare,
        CloseSquare,

        Dot,
        Comma,
        Colon,
        Semicolon,

        Operator
    }
}
