namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,
        EndOfFile,

        LineBreak,
        Print,

        LiteralNumeric,
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
        Not,
        Or,
        And,

        Bar,
        Amp,

        PlusPlus,
        MinusMinus,

        Colon, SemiColon, Dot, Comma,
    }

  

    public static class TokenTypeEx
    {
        public static EOperatorType Type(this ETokenType type)
        {
            switch (type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                case ETokenType.Star:
                case ETokenType.Slash:
                case ETokenType.Caret:
                    return EOperatorType.Arythmetic;

                case ETokenType.Less:
                case ETokenType.LessEqual:
                case ETokenType.Greater:
                case ETokenType.GreaterEqual:
                    return EOperatorType.Comparison;

                case ETokenType.Equal:
                case ETokenType.NotEqual:
                    return EOperatorType.Equality;

                case ETokenType.Or:
                case ETokenType.Not:
                case ETokenType.And:
                    return EOperatorType.Boolean;
            }
            return EOperatorType.None;
        }
    }
}
