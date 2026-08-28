namespace TriUgla.Script;

public sealed class Tokenizer(string source)
{
    readonly string _source = source ?? throw new ArgumentNullException(nameof(source));
    int _position;
    int _line = 1;
    int _column = 1;

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!AtEnd)
        {
            SkipTrivia();
            if (!AtEnd)
            {
                tokens.Add(ReadToken());
            }
        }

        tokens.Add(new Token(
            TokenKind.EndOfFile,
            string.Empty,
            new TextSpan(_position, 0, _line, _column)));
        return tokens;
    }

    Token ReadToken()
    {
        int start = _position;
        int line = _line;
        int column = _column;

        if (IsIdentifierStart(Current))
        {
            Advance();
            while (IsIdentifierPart(Current))
            {
                Advance();
            }

            string text = _source[start.._position];
            return Keywords.TryGetKind(text, out KeywordKind keyword)
                ? MakeToken(TokenKind.Keyword, start, line, column, keyword)
                : MakeToken(TokenKind.Identifier, start, line, column);
        }

        if (char.IsDigit(Current) || Current == '.' && char.IsDigit(Peek(1)))
        {
            return ReadNumber(start, line, column);
        }

        if (Current == '"')
        {
            return ReadString(start, line, column);
        }

        TokenKind kind = Current switch
        {
            '(' => TokenKind.LeftParenthesis,
            ')' => TokenKind.RightParenthesis,
            '{' => TokenKind.LeftBrace,
            '}' => TokenKind.RightBrace,
            '[' => TokenKind.LeftBracket,
            ']' => TokenKind.RightBracket,
            ',' => TokenKind.Comma,
            ':' => TokenKind.Colon,
            ';' => TokenKind.Semicolon,
            '.' => TokenKind.Dot,
            '+' => TokenKind.Plus,
            '-' => TokenKind.Minus,
            '*' => TokenKind.Star,
            '/' => TokenKind.Slash,
            '%' => TokenKind.Percent,
            '=' => MatchNext('=') ? TokenKind.EqualsEquals : TokenKind.Equals,
            '!' => MatchNext('=') ? TokenKind.BangEquals : TokenKind.Bang,
            '<' => MatchNext('=') ? TokenKind.LessOrEquals : TokenKind.Less,
            '>' => MatchNext('=') ? TokenKind.GreaterOrEquals : TokenKind.Greater,
            _ => TokenKind.BadToken
        };

        Advance();
        return MakeToken(kind, start, line, column);
    }

    Token ReadNumber(int start, int line, int column)
    {
        while (char.IsDigit(Current))
        {
            Advance();
        }

        if (Current == '.')
        {
            Advance();
            while (char.IsDigit(Current))
            {
                Advance();
            }
        }

        if (Current is 'e' or 'E')
        {
            int exponentStart = _position;
            int exponentColumn = _column;
            Advance();
            if (Current is '+' or '-')
            {
                Advance();
            }

            if (!char.IsDigit(Current))
            {
                _position = exponentStart;
                _column = exponentColumn;
                return MakeToken(TokenKind.Number, start, line, column);
            }

            while (char.IsDigit(Current))
            {
                Advance();
            }
        }

        return MakeToken(TokenKind.Number, start, line, column);
    }

    Token ReadString(int start, int line, int column)
    {
        Advance();

        while (!AtEnd && Current != '"' && Current is not '\r' and not '\n')
        {
            if (Current == '\\' && !AtEndAt(1))
            {
                Advance();
            }

            Advance();
        }

        TokenKind kind = Current == '"' ? TokenKind.String : TokenKind.BadToken;
        if (Current == '"')
        {
            Advance();
        }

        return MakeToken(kind, start, line, column);
    }

    void SkipTrivia()
    {
        while (true)
        {
            while (char.IsWhiteSpace(Current))
            {
                Advance();
            }

            if (Current == '/' && Peek(1) == '/')
            {
                while (!AtEnd && Current is not '\r' and not '\n')
                {
                    Advance();
                }

                continue;
            }

            if (Current == '/' && Peek(1) == '*')
            {
                Advance();
                Advance();
                while (!AtEnd && !(Current == '*' && Peek(1) == '/'))
                {
                    Advance();
                }

                if (!AtEnd)
                {
                    Advance();
                    Advance();
                }

                continue;
            }

            return;
        }
    }

    Token MakeToken(
        TokenKind kind,
        int start,
        int line,
        int column,
        KeywordKind keyword = KeywordKind.None)
        => new(
            kind,
            _source[start.._position],
            new TextSpan(start, _position - start, line, column),
            keyword);

    bool MatchNext(char expected)
    {
        if (Peek(1) != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    void Advance()
    {
        if (AtEnd)
        {
            return;
        }

        if (Current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
    }

    char Current => Peek(0);

    char Peek(int offset)
        => AtEndAt(offset) ? '\0' : _source[_position + offset];

    bool AtEnd => _position >= _source.Length;

    bool AtEndAt(int offset) => _position + offset >= _source.Length;

    static bool IsIdentifierStart(char value)
        => char.IsLetter(value) || value == '_';

    static bool IsIdentifierPart(char value)
        => char.IsLetterOrDigit(value) || value == '_';
}
