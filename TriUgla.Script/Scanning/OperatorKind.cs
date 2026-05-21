namespace TriUgla.Script.Scanning
{
    public enum OperatorKind
    {
        None,

        Assign,

        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        Power,

        PlusAssign,
        MinusAssign,
        MultiplyAssign,
        DivideAssign,

        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,

        And,
        Or,
        Not
    }
}
