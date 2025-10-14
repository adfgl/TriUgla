
namespace TriUgla.Parsing.Scanning
{
    public class Scanner
    {
        public const int LOOKAHEAD_SIZE = 10;

        readonly TokenReader _reader;
        readonly Token[] _lookaheadBuffer = new Token[LOOKAHEAD_SIZE];

        int _bufferStart = 0;
        int _bufferCount = 0;

        public Scanner(string source)
        {
            _reader = new TokenReader(source);
        }

        public List<Token> ReadAll(bool ignoreComments = false)
        {
            List<Token> tokens = new List<Token>();
            Token token;

            int line = -1;
            do
            {
                token = Consume();
                if (ignoreComments && (token.type == ETokenType.Comment))
                {
                    continue;
                }

                if (token.line != line)
                {
                    line = token.line;
                    Console.Write($"{token.line + 1:D4} ");
                }
                else
                {
                    Console.Write("   | ");
                }

                Console.WriteLine($"{token.column + 1:D3} " + token.type + " " + token.value);

                tokens.Add(token);
            } while (token.type != ETokenType.EOF);
            return tokens;
        }

        public Token Consume()
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
            return _reader.Read();
        }

        public Token Peek(int offset = 0)
        {
            // Ensure the buffer has enough tokens to satisfy the peek request
            while (_bufferCount <= offset)
            {
                if (_bufferCount >= LOOKAHEAD_SIZE)
                {
                    throw new InvalidOperationException("Lookahead buffer size exceeded.");
                }

                // Add the next token to the buffer
                int insertIndex = _bufferStart + _bufferCount;
                if (insertIndex >= LOOKAHEAD_SIZE)
                {
                    insertIndex -= LOOKAHEAD_SIZE; // Wrap around to the beginning
                }

                Token token = _reader.Read();
                _lookaheadBuffer[insertIndex] = token;
                _bufferCount++;

                if (token.type == ETokenType.EOF)
                {
                    break;
                }
            }

            // Retrieve the token at the specified offset
            int peekIndex = _bufferStart + offset;
            if (peekIndex >= LOOKAHEAD_SIZE)
            {
                peekIndex -= LOOKAHEAD_SIZE;
            }
            return _lookaheadBuffer[peekIndex];
        }
    }
}
