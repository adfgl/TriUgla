namespace TriScript.Scanning
{
    public sealed class Scanner
    {
        public const int LOOKAHEAD_SIZE = 10;
        readonly Token[] _lookaheadBuffer = new Token[LOOKAHEAD_SIZE];

        int _bufferStart = 0;
        int _bufferCount = 0;

        TokenReader _reader;
        Token _previous = new Token();

        public Scanner(Source source)
        {
            _reader = new TokenReader(source);
        }

        public Source Source => _reader.Source;
        public Token Previous => _previous;

        public List<Token> ReadAll(params ETokenType[] stoppers)
        {
            List<Token> tokens = new List<Token>(256);
            while (true)
            {
                Token t = _reader.Read();
                tokens.Add(t);

                if (t.type == ETokenType.EndOfFile || stoppers.Contains(t.type))
                    break;
            }
            return tokens;
        }

        public Token Consume()
        {
            Token token;
            if (_bufferCount > 0)
            {
                token = _lookaheadBuffer[_bufferStart++];
                if (_bufferStart == LOOKAHEAD_SIZE)
                {
                    _bufferStart = 0;
                }
                _bufferCount--;

                _previous = token;
                return token;
            }
            else
            {
                token = _reader.Read();
            }
            _previous = token;
            return token;
        }

        public Token Peek(int offset = 0)
        {
            while (_bufferCount <= offset)
            {
                if (_bufferCount >= LOOKAHEAD_SIZE)
                {
                    throw new InvalidOperationException("Lookahead buffer size exceeded.");
                }

                int insertIndex = _bufferStart + _bufferCount;
                if (insertIndex >= LOOKAHEAD_SIZE)
                {
                    insertIndex -= LOOKAHEAD_SIZE;
                }

                Token token = _reader.Read();
                _lookaheadBuffer[insertIndex] = token;
                _bufferCount++;

                if (token.type == ETokenType.EndOfFile)
                {
                    break;
                }
            }

            int peekIndex = _bufferStart + offset;
            if (peekIndex >= LOOKAHEAD_SIZE)
            {
                peekIndex -= LOOKAHEAD_SIZE;
            }
            return _lookaheadBuffer[peekIndex];
        }
    }
}
