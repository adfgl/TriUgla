namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,

        LiteralInteger,
        LiteralReal,
        LiteralString,
        LiteralSymbol,

        Plus, Minus, Start, Slash, Caret,

        Less, LessEqual,
        Greater, GreaterEqual,
        Equal, NotEqual,
    }
}
