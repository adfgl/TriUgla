namespace TriUgla.Script;

public sealed class TokenReader
{
    readonly IReadOnlyList<Token> _tokens;
    int _position;
    Token? _lookAhead;

    public int Position => _position;

    public TokenReader(string source)
        : this(new Tokenizer(source).Tokenize())
    {
    }

    public TokenReader(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (tokens.Count == 0 || tokens[^1].Kind != TokenKind.EndOfFile)
        {
            throw new ArgumentException(
                "The token sequence must end with an end-of-file token.",
                nameof(tokens));
        }

        _tokens = tokens;
    }

    public Token Peek()
    {
        _lookAhead ??= Current;
        return _lookAhead.Value;
    }

    public Token Peek(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return offset == 0
            ? Peek()
            : _tokens[Math.Min(_position + offset, _tokens.Count - 1)];
    }

    public Token Read()
    {
        Token token = _lookAhead ?? Current;
        _lookAhead = null;

        if (token.Kind != TokenKind.EndOfFile)
        {
            _position++;
        }

        return token;
    }

    Token Current => _tokens[Math.Min(_position, _tokens.Count - 1)];
}
