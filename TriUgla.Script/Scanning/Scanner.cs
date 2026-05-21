namespace TriUgla.Script.Scanning
{
    public sealed class Scanner
    {
        readonly Cursor _cursor;
        readonly TokenReader _reader;
        readonly List<Token> _lookahead = [];

        Token _previous;

        public Scanner(Source source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _cursor = new Cursor(source);
            _reader = new TokenReader(_cursor);
        }

        public Source Source => _cursor.Source;
        public Token Previous => _previous;

        public Token Consume()
        {
            Token token = Peek();

            if (_lookahead.Count > 0)
                _lookahead.RemoveAt(0);

            _previous = token;
            return token;
        }

        public Token Peek(int offset = 0)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            Fill(offset);

            return offset < _lookahead.Count
                ? _lookahead[offset]
                : _lookahead[^1];
        }

        void Fill(int offset)
        {
            while (_lookahead.Count <= offset)
            {
                Token token = ReadSignificant();
                _lookahead.Add(token);

                if (token.Kind == TokenKind.EndOfFile)
                    break;
            }
        }

        Token ReadSignificant()
        {
            while (true)
            {
                Token token = _reader.Read();

                if (token.Kind is TokenKind.Comment or TokenKind.MultiLineComment)
                    continue;

                return token;
            }
        }

        public IEnumerable<Token> ReadAll(bool includeComments = false)
        {
            while (true)
            {
                Token token = includeComments
                    ? _reader.Read()
                    : Consume();

                yield return token;

                if (token.Kind == TokenKind.EndOfFile)
                    yield break;
            }
        }

        public string Text(Token token) => Text(token.Span);
        public string Text(Span span) => _cursor.Text(span);

        public string LineText(Token token) => LineText(token.Position);
        public string LineText(Position position) => _cursor.GetLineText(position.Line);

        public string Marker(Token token) => _cursor.MakeMarker(token.Position, token.Span);
    }
}
