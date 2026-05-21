namespace TriUgla.Script.Scanning
{
    public readonly record struct Token(
        Source Source,
        TokenKind Kind,
        Position Position,
        Span Span,
        int Value = 0)
    {
        public string Text => Source.Slice(Span);

        public bool IsError => Kind == TokenKind.Error;
        public bool IsKeyword => Kind == TokenKind.Keyword;
        public bool IsOperator => Kind == TokenKind.Operator;
        public bool IsEnd => Kind == TokenKind.EndOfFile;

        public Keyword Keyword
        {
            get
            {
                if (Kind != TokenKind.Keyword)
                    throw new InvalidOperationException($"Token is not keyword: {Kind}.");

                return (Keyword)Value;
            }
        }

        public OperatorKind Operator
        {
            get
            {
                if (Kind != TokenKind.Operator)
                    throw new InvalidOperationException($"Token is not operator: {Kind}.");

                return (OperatorKind)Value;
            }
        }

        public ScanError ScanError
        {
            get
            {
                if (Kind != TokenKind.Error)
                    throw new InvalidOperationException($"Token is not error: {Kind}.");

                return (ScanError)Value;
            }
        }

        public bool Is(TokenKind kind)
        {
            return Kind == kind;
        }

        public bool Is(Keyword keyword)
        {
            return Kind == TokenKind.Keyword &&
                   (Keyword)Value == keyword;
        }

        public bool Is(OperatorKind op)
        {
            return Kind == TokenKind.Operator &&
                   (OperatorKind)Value == op;
        }

        public override string ToString()
        {
            return Kind switch
            {
                TokenKind.Keyword => $"Keyword.{Keyword} '{Text}'",
                TokenKind.Operator => $"Operator.{Operator} '{Text}'",
                TokenKind.Error => $"Error.{ScanError} '{Text}'",
                _ => $"{Kind} '{Text}'"
            };
        }
    }
}
