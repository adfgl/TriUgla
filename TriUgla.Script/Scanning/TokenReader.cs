namespace TriUgla.Script.Scanning
{

    public sealed class TokenReader(Cursor cursor)
    {
        public Token Read()
        {
            SkipWhiteSpaceOnly();

            int start = cursor.Index;
            Position position = cursor.Position;

            if (cursor.IsEnd)
                return Make(TokenKind.EndOfFile, position, start);

            char ch = cursor.Current;

            if (IsLineBreak(ch))
            {
                cursor.Advance();
                return Make(TokenKind.LineBreak, position, start);
            }

            if (ch == '/' && cursor.Peek(1) == '/')
                return LineComment(position, start);

            if (ch == '/' && cursor.Peek(1) == '*')
                return MultiLineComment(position, start);

            if (IsIdentifierStart(ch))
                return Identifier(position, start);

            if (IsNumberStart(ch))
                return Number(position, start);

            if (ch is '"' or '\'')
                return String(position, start);

            return SymbolOrOperator(position, start);
        }

        void SkipWhiteSpaceOnly()
        {
            while (!cursor.IsEnd &&
                   char.IsWhiteSpace(cursor.Current) &&
                   !IsLineBreak(cursor.Current))
            {
                cursor.Advance();
            }
        }

        Token LineComment(Position position, int start)
        {
            cursor.Advance(2);

            while (!cursor.IsEnd && !IsLineBreak(cursor.Current))
                cursor.Advance();

            return Make(TokenKind.Comment, position, start);
        }

        Token MultiLineComment(Position position, int start)
        {
            cursor.Advance(2);

            while (!cursor.IsEnd)
            {
                if (cursor.Current == '*' && cursor.Peek(1) == '/')
                {
                    cursor.Advance(2);
                    return Make(TokenKind.MultiLineComment, position, start);
                }

                cursor.Advance();
            }

            return Error(ScanError.UnterminatedComment, position, start);
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
                    cursor.Source,
                    TokenKind.Keyword,
                    position,
                    span,
                    (int)keyword);
            }

            return new Token(
                cursor.Source,
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

                if (cursor.IsEnd || !IsDigit(cursor.Current))
                    return Error(ScanError.InvalidNumber, position, start);

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
                    return Error(ScanError.UnterminatedString, position, start);

                if (ch == quote)
                {
                    cursor.Advance();
                    return Make(TokenKind.String, position, start);
                }

                if (ch == '\\')
                {
                    cursor.Advance();

                    if (cursor.IsEnd)
                        return Error(ScanError.UnterminatedString, position, start);

                    if (!IsValidEscape(cursor.Current))
                    {
                        cursor.Advance();
                        return Error(ScanError.InvalidEscape, position, start);
                    }
                }

                cursor.Advance();
            }

            return Error(ScanError.UnterminatedString, position, start);
        }

        static bool IsValidEscape(char c)
        {
            return c is 'n' or 'r' or 't' or '\\' or '"' or '\'';
        }

        Token SymbolOrOperator(Position position, int start)
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
            return Error(ScanError.UnknownCharacter, position, start);
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
                return Error(ScanError.InvalidOperator, position, start);

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
                 cursor.Source,
                kind,
                position,
                cursor.SpanFrom(start),
                value);
        }

        Token Error(
            ScanError error,
            Position position,
            int start)
        {
            return new Token(
                 cursor.Source,
                TokenKind.Error,
                position,
                cursor.SpanFrom(start),
                (int)error);
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
