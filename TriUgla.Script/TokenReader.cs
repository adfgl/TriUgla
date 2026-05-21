namespace TriUgla.Script
{
    public readonly record struct Position(int Line, int Column)
    {
        public override string ToString()
            => $"Ln: {Line + 1} Ch: {Column + 1}";
    }

    public readonly record struct Span(int Start, int Length)
    {
        public int End => Start + Length;
        public override string ToString() 
            => $"{Start + 1}..{End + 1}";
    }

    public sealed class TokenReader(Cursor cursor)
    {
        public Token Read()
        {
            SkipTrivia();

            int start = cursor.Index;
            Position position = cursor.Position;

            if (cursor.IsEnd)
            {
                return Make(
                    TokenKind.EndOfFile,
                    position,
                    start);
            }

            char ch = cursor.Current;

            if (IsLineBreak(ch))
            {
                cursor.Advance();

                return Make(
                    TokenKind.LineBreak,
                    position,
                    start);
            }

            if (IsIdentifierStart(ch))
                return Identifier(position, start);

            if (IsNumberStart(ch))
                return Number(position, start);

            if (ch is '"' or '\'')
                return String(position, start);

            return Symbol(position, start);
        }

        void SkipTrivia()
        {
            while (true)
            {
                if (false ==cursor.SkipWhiteSpace())
                    break;
            }
        }

        Token Identifier(Position position, int start)
        {
            cursor.Advance();

            while (!cursor.IsEnd && IsIdentifierPart(cursor.Current))
                cursor.Advance();

            Span span = cursor.SpanFrom(start);

            string text = cursor.Text(span);

            if (Keywords.Source.TryGetValue(text, out Keyword keyword))
            {
                return new Token(
                    TokenKind.Keyword,
                    position,
                    span,
                    (int)keyword);
            }

            return new Token(
                TokenKind.Identifier,
                position,
                span);
        }


        Token Number(Position position, int start)
        {
            while (!cursor.IsEnd && IsDigit(cursor.Current))
                cursor.Advance();

            if (!cursor.IsEnd &&
                cursor.Current == '.' &&
                IsDigit(cursor.Peek(1)))
            {
                cursor.Advance();

                while (!cursor.IsEnd && IsDigit(cursor.Current))
                    cursor.Advance();
            }

            if (!cursor.IsEnd && cursor.Current is 'e' or 'E')
            {
                cursor.Advance();

                if (!cursor.IsEnd && cursor.Current is '+' or '-')
                    cursor.Advance();

                while (!cursor.IsEnd && IsDigit(cursor.Current))
                    cursor.Advance();
            }

            return Make(TokenKind.Number, position, start);
        }

        Token String(Position position, int start)
        {
            char quote = cursor.Current;

            cursor.Advance();

            while (!cursor.IsEnd)
            {
                char ch = cursor.Current;

                if (IsLineBreak(ch))
                    return Make(TokenKind.Undefined, position, start);

                if (ch == quote)
                {
                    cursor.Advance();
                    return Make(TokenKind.String, position, start);
                }

                if (ch == '\\')
                {
                    cursor.Advance();

                    if (cursor.IsEnd)
                        return Make(TokenKind.Undefined, position, start);
                }

                cursor.Advance();
            }

            return Make(TokenKind.Undefined, position, start);
        }

        Token Symbol(Position position, int start)
        {
            TokenKind kind = cursor.Current switch
            {
                '(' => TokenKind.OpenParen,
                ')' => TokenKind.CloseParen,

                '{' => TokenKind.OpenCurly,
                '}' => TokenKind.CloseCurly,

                '[' => TokenKind.OpenSquare,
                ']' => TokenKind.CloseSquare,

                ',' => TokenKind.Comma,
                ':' => TokenKind.Colon,
                ';' => TokenKind.Semicolon,

                _ => TokenKind.Undefined
            };

            if (kind != TokenKind.Undefined)
            {
                cursor.Advance();
                return Make(kind, position, start);
            }

            return Operator(position, start);
        }

        Token Operator(Position position, int start)
        {
            char ch = cursor.Current;

            switch (ch)
            {
                case '+':
                    return Operator2('=', OperatorKind.PlusAssign, OperatorKind.Plus, position, start);

                case '-':
                    return Operator2('=', OperatorKind.MinusAssign, OperatorKind.Minus, position, start);

                case '*':
                    return Operator2('=', OperatorKind.MultiplyAssign, OperatorKind.Multiply, position, start);

                case '/':
                    return Operator2('=', OperatorKind.DivideAssign, OperatorKind.Divide, position, start);

                case '%':
                    cursor.Advance();
                    return Make(TokenKind.Operator, position, start, (int)OperatorKind.Modulo);

                case '^':
                    cursor.Advance();
                    return Make(TokenKind.Operator, position, start, (int)OperatorKind.Power);

                case '=':
                    return Operator2('=', OperatorKind.Equal, OperatorKind.Assign, position, start);

                case '!':
                    return Operator2('=', OperatorKind.NotEqual, OperatorKind.Not, position, start);

                case '<':
                    return Operator2('=', OperatorKind.LessEqual, OperatorKind.Less, position, start);

                case '>':
                    return Operator2('=', OperatorKind.GreaterEqual, OperatorKind.Greater, position, start);

                case '&':
                    return Required2('&', OperatorKind.And, position, start);

                case '|':
                    return Required2('|', OperatorKind.Or, position, start);
            }

            cursor.Advance();
            return Make(TokenKind.Undefined, position, start);
        }

        Token Operator2(
            char second,
            OperatorKind twoChar,
            OperatorKind oneChar,
            Position position,
            int start)
        {
            cursor.Advance();

            if (!cursor.IsEnd && cursor.Current == second)
            {
                cursor.Advance();
                return Make(TokenKind.Operator, position, start, (int)twoChar);
            }

            return Make(TokenKind.Operator, position, start, (int)oneChar);
        }

        Token Required2(
            char second,
            OperatorKind kind,
            Position position,
            int start)
        {
            cursor.Advance();

            if (cursor.IsEnd || cursor.Current != second)
                return Make(TokenKind.Undefined, position, start);

            cursor.Advance();
            return Make(TokenKind.Operator, position, start, (int)kind);
        }

        Token Make(
            TokenKind kind,
            Position position,
            int start,
            int value = 0)
        {
            return new Token(
                kind,
                position,
                cursor.SpanFrom(start),
                value);
        }

        static bool IsDigit(char c) 
            => c is >= '0' and <= '9';

        static bool IsIdentifierStart(char c) 
            => char.IsLetter(c) || c == '_';

        static bool IsIdentifierPart(char c)
            => char.IsLetterOrDigit(c) || c == '_';

        bool IsNumberStart(char c)
            => IsDigit(c) ||
                   c == '.' && IsDigit(cursor.Peek(1));

        static bool IsLineBreak(char c) => c is '\r' or '\n';
    }
}
