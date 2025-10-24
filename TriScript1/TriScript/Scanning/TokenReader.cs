using System.Runtime.CompilerServices;

namespace TriScript.Scanning
{
    public class TokenReader
    {
        const char EOF = '\0';
        int _pos, _line, _col;

        public TokenReader(Source source)
        {
            Source = source;
            Reset();
        }

        public Source Source { get; set; }
        public TextPosition Position => new TextPosition(_line, _col);

        public void Reset()
        {
            _pos = _line = _col = 0;
        }

        public Token Read()
        {
            SkipSpace();
            char ch = Peek();

            if (ch == EOF) return ReadEndOfFile();
            if (ch == '\n') return ReadNewline();
            if (ch == '\"' || ch == '\'') return ReadString(ETokenType.LiteralString, ch);
            if (IsDigit(ch) || (ch == '.' && IsDigit(Peek(1)))) return ReadNumber();
            if (IsIdentStart(ch)) return ReadIdentifier();
            return ReadOperatorOrPunct();
        }

        Token ReadOperatorOrPunct()
        {
            int line = _line, col = _col, pos = _pos;

            ETokenType type;
            switch (Peek())
            {
                case '^':
                    type = ETokenType.Caret;
                    Advance();
                    break;

                case '.':
                    type = ETokenType.Dot;
                    Advance();
                    break;

                case ',':
                    type = ETokenType.Comma;
                    Advance();
                    break;

                case ':':
                    type = ETokenType.Colon;
                    Advance();
                    break;

                case ';':
                    type = ETokenType.SemiColon;
                    Advance();
                    break;

                case '|':
                    type = ETokenType.Bar;
                    Advance();
                    if (Peek() == '|')
                    {
                        type = ETokenType.Or;
                        Advance();
                    }
                    break;

                case '&':
                    type = ETokenType.Amp;
                    Advance();
                    if (Peek() == '&')
                    {
                        type = ETokenType.And;
                        Advance();
                    }
                    break;

                case '>':
                    type = ETokenType.Greater;
                    Advance();
                    if (Peek() == '=')
                    {
                        type = ETokenType.GreaterEqual;
                        Advance();
                    }
                    break;

                case '<':
                    type = ETokenType.Less;
                    Advance();
                    if (Peek() == '=')
                    {
                        type = ETokenType.LessEqual;
                        Advance();
                    }
                    break;

                case '!':
                    type = ETokenType.Not;
                    Advance();
                    if (Peek() == '=')
                    {
                        type = ETokenType.NotEqual;
                        Advance();
                    }
                    break;

                case '=':
                    type = ETokenType.Assign;
                    Advance();
                    if (Peek() == '=')
                    {
                        type = ETokenType.Equal;
                        Advance();
                    }
                    break;

                case '*':
                    type = ETokenType.Star;
                    Advance();
                    break;

                case '/':
                    type = ETokenType.Slash;
                    Advance();
                    break;

                case '+':
                    type = ETokenType.Plus;
                    Advance();
                    if (Peek() == '+')
                    {
                        type = ETokenType.PlusPlus;
                        Advance();
                    }
                    break;

                case '-':
                    type = ETokenType.Minus;
                    Advance();
                    if (Peek() == '-')
                    {
                        type = ETokenType.MinusMinus;
                        Advance();
                    }
                    break;

                case '(':
                    type = ETokenType.OpenParen;
                    Advance();
                    break;
                case '[':
                    type = ETokenType.OpenSquare;
                    Advance();
                    break;
                case '{':
                    type = ETokenType.OpenCurly;
                    Advance();
                    break;

                case ')':
                    type = ETokenType.CloseParen;
                    Advance();
                    break;
                case ']':
                    type = ETokenType.CloseSquare;
                    Advance();
                    break;
                case '}':
                    type = ETokenType.CloseCurly;
                    Advance();
                    break;

                default:
                    type = ETokenType.Undefined;
                    Advance();
                    break;
            }
            return new Token(type, new TextPosition(line, col), new TextSpan(pos, _pos - pos));
        }

        Token ReadNumber()
        {
            int line = _line, col = _col, start = _pos;

            if (Peek() == '.') Advance();
            else while (Char.IsDigit(Peek())) Advance();

            if (Peek() == '.') { Advance(); while (IsDigit(Peek())) Advance(); }

            if (Peek() is 'e' or 'E')
            {
                Advance();
                if (Peek() is '+' or '-') Advance();
                while (IsDigit(Peek())) Advance();
            }

            TextSpan span = new TextSpan(start, _pos - start);
            return new Token(ETokenType.LiteralNumeric, new TextPosition(line, col), span);
        }

        Token ReadIdentifier()
        {
            int line = _line, col = _col, start = _pos;
            if (!IsIdentStart(Advance())) throw new Exception();
            while (IsIdentPart(Peek())) Advance();
            TextSpan span = new TextSpan(start, _pos - start);
            string value = Source.GetString(span);

            ETokenType type = ETokenType.LiteralSymbol;
            if (Keywords.Source.TryGetValue(value, out ETokenType keyword))
            {
                type = keyword;
            }
            return new Token(type, new TextPosition(line, col), span);
        }

        Token ReadString(ETokenType type, char quates)
        {
            Advance();
            int line = _line, col = _col, start = _pos;

            while (true)
            {
                char ch = Peek();
                if (ch == EOF) break;
                if (ch == quates) break;
                Advance();
            }

            TextSpan span = new TextSpan(start, _pos - start);
            Advance();
            return new Token(type, new TextPosition(line, col), span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDigit(char c) => (uint)(c - '0') <= 9u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        Token ReadEndOfFile()
        {
            Token tkn = new Token(ETokenType.EndOfFile, Position, new TextSpan(_pos, 0));
            return tkn;
        }

        Token ReadNewline()
        {
            Token tkn = new Token(ETokenType.LineBreak, Position, new TextSpan(_pos, 0));
            Advance();
            return tkn;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        char Advance()
        {
            char ch = Source.Content[_pos++];
            if (ch != '\n')
            {
                _col++;
            }
            else
            {
                _col = 0;
                _line++;
            }
            return ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        char Peek(int offset = 0)
        {
            string src = Source.Content;
            if (_pos + offset < src.Length)
            {
                return src[_pos + offset];
            }
            return EOF;
        }

        void SkipSpace()
        {
            while (true)
            {
                char ch = Peek();
                if (ch == ' ' || ch == '\t' || ch == '\r') { Advance(); }
                else break;
            }
        }
    }
}
