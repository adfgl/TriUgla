using TriScript.Diagnostics;

namespace TriScript.Scanning
{
    public class Scanner
    {
        public const int LOOKAHEAD_SIZE = 10;
        readonly Token[] _lookaheadBuffer = new Token[LOOKAHEAD_SIZE];

        int _bufferStart = 0;
        int _bufferCount = 0;

        public Scanner(TokenReader reader)
        {
            Reader = reader;
        }

        public TokenReader Reader { get; set; }

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

        public Token Consume(DiagnosticBag? diagnostic)
        {
            if (_bufferCount > 0)
            {
                Token token = _lookaheadBuffer[_bufferStart++];
                if (_bufferStart == LOOKAHEAD_SIZE)
                {
                    _bufferStart = 0;
                }
                _bufferCount--;
                return token;
            }
            return Reader.Read(diagnostic);
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

                Token token = Reader.Read(diagnostic);
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
