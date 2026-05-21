namespace TriUgla.Script
{
    public sealed class Scanner(TokenReader reader)
    {
        readonly List<Token> _lookahead = [];

        Token _previous = default;

        public Token Previous => _previous;

        public Token Consume()
        {
            Token token = Peek(0);

            _lookahead.RemoveAt(0);

            _previous = token;
            return token;
        }

        public Token Peek(int offset = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            Fill(offset);

            return _lookahead[offset];
        }

        public bool Match(TokenKind kind)
        {
            if (Peek().Kind != kind)
                return false;

            Consume();
            return true;
        }

        public bool MatchOperator(OperatorKind kind)
        {
            Token token = Peek();

            if (token.Kind != TokenKind.Operator)
                return false;

            if ((OperatorKind)token.Value != kind)
                return false;

            Consume();
            return true;
        }

        public IEnumerable<Token> ReadAll()
        {
            while (true)
            {
                Token token = Consume();
                yield return token;

                if (token.Kind == TokenKind.EndOfFile)
                    yield break;
            }
        }

        void Fill(int offset)
        {
            while (_lookahead.Count <= offset)
            {
                Token token = reader.Read();
                _lookahead.Add(token);

                if (token.Kind == TokenKind.EndOfFile)
                    break;
            }
        }
    }
}
