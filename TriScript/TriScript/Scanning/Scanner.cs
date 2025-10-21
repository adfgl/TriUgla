using TriScript.Diagnostics;

namespace TriScript.Scanning
{
    public class Scanner
    {
        public const int LOOKAHEAD_SIZE = 10;
        readonly Token[] _lookaheadBuffer = new Token[LOOKAHEAD_SIZE];

        int _bufferStart = 0;
        int _bufferCount = 0;

        TokenReader _reader;

        public Scanner(Source source)
        {
            _reader = new TokenReader(source);
        }

        public Source Source => _reader.Source;

        public List<Token> ReadAll(DiagnosticBag? diagnostic)
        {
            List<Token> tokens = new List<Token>();
            while (true)
            {
                var token = Consume(diagnostic);
                tokens.Add(token);
                if (token.type == ETokenType.EndOfFile)
                    break;
            }
            return tokens;
        }

        Token _previous = new Token();

        public Token Previous => _previous;

        public Token Consume(DiagnosticBag? diagnostic)
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
                token = _reader.Read(diagnostic);
            }
            _previous = token;
            return token;
        }

        public Token Peek(DiagnosticBag? diagnostic, int offset = 0)
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

                Token token = _reader.Read(diagnostic);
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
