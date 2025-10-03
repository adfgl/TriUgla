using System.Globalization;
using System.Runtime.CompilerServices;

namespace TriUgla.Parsing.Scanning
{
    public sealed class TokenReader
    {
        private const char EOF = '\0';
        private readonly string _src;
        private int _pos, _line, _col;

        public int Current => _pos;
        public int Line => _line;
        public int Column => _col;

        public TokenReader(string source) => _src = source ?? "";

        public Token Read()
        {
            SkipSpace();
            char ch = Peek();
            if (ch == EOF) return new Token(ETokenType.EOF, _line, _col, "");

            if (ch == '\n') return ReadNewline();
            if (ch == '/' && Peek(1) == '/') return ReadLineComment();
            if (IsDigit(ch) || (ch == '.' && IsDigit(Peek(1)))) return ReadNumber();
            if (IsIdentStart(ch)) return ReadIdentifier();
            if (ch == '"') return ReadString();

            return ReadOperatorOrPunct();
        }

        // ----- helpers -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Peek(int k = 0) => (_pos + k) < _src.Length ? _src[_pos + k] : EOF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Advance()
        {
            char c = _src[_pos++];
            if (c != '\n') _col++;
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char c) => (uint)(c - '0') <= 9u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        private void SkipSpace()
        {
            while (true)
            {
                char c = Peek();
                if (c == ' ' || c == '\t' || c == '\r') { Advance(); }
                else break;
            }
        }

        private Token ReadNewline()
        {
            int line = _line, col = _col;
            Advance(); _line++; _col = 0;
            return new Token(ETokenType.LineBreak, line, col, "");
        }

        private Token ReadLineComment()
        {
            int line = _line, col = _col;
            Advance(); Advance(); // //
            int start = _pos;
            while (true)
            {
                char c = Peek();
                if (c == EOF || c == '\n') break;
                Advance();
            }
            int len = _pos - start + 2;
            string text = _src.Substring(start, len - 2); // keep just the comment text (no //)
            return new Token(ETokenType.Comment, line, col, text);
        }

        private Token ReadIdentifier()
        {
            int line = _line, col = _col, start = _pos;
            Advance();
            while (IsIdentPart(Peek())) Advance();
            int len = _pos - start;
            string text = _src.Substring(start, len);
            return new Token(ETokenType.IdentifierLiteral, line, col, text);
        }

        private Token ReadNumber()
        {
            int line = _line, col = _col, start = _pos;

            if (Peek() == '.') Advance();
            else while (IsDigit(Peek())) Advance();

            if (Peek() == '.') { Advance(); while (IsDigit(Peek())) Advance(); }

            if (Peek() is 'e' or 'E')
            {
                Advance();
                if (Peek() is '+' or '-') Advance();
                while (IsDigit(Peek())) Advance();
            }

            int len = _pos - start;
            string lexeme = _src.Substring(start, len);

            if (double.TryParse(lexeme, out double num))
            {
                return new Token(ETokenType.NumericLiteral, line, col, lexeme, num);
            }
            return new Token(ETokenType.Error, line, col, $"Couldn't parse {lexeme}.");
        }

        private Token ReadString()
        {
            int line = _line, col = _col, start = _pos; // start at opening quote
            Advance(); // consume opening "

            while (true)
            {
                char c = Peek();
                if (c == EOF || c == '\n') break;      // unterminated: raw up to EOL/EOF
                Advance();
                if (c == '"') break;                   // consumed closing quote, done
                if (c == '\\')                         // skip escaped char (raw)
                {
                    char e = Peek();
                    if (e == EOF || e == '\n') break;
                    Advance();
                }
            }

            int len = _pos - start;
            string lexeme = _src.Substring(start, len); 
            return new Token(ETokenType.StringLiteral, line, col, lexeme);
        }

        private Token ReadOperatorOrPunct()
        {
            int line = _line, col = _col;
            char a = Peek(), b = Peek(1);

            // two-char ops
            if (b != EOF)
            {
                if (a == '!' && b == '=') { Advance(); Advance(); return new Token(ETokenType.NotEqual, line, col, ""); }
                if (a == '=' && b == '=') { Advance(); Advance(); return new Token(ETokenType.EqualEqual, line, col, ""); }
                if (a == '>' && b == '=') { Advance(); Advance(); return new Token(ETokenType.GreaterOrEqual, line, col, ""); }
                if (a == '<' && b == '=') { Advance(); Advance(); return new Token(ETokenType.LessOrEqual, line, col, ""); }
                if (a == '&' && b == '&') { Advance(); Advance(); return new Token(ETokenType.And, line, col, ""); }
                if (a == '|' && b == '|') { Advance(); Advance(); return new Token(ETokenType.Or, line, col, ""); }
            }

            // single-char
            Advance();
            return a switch
            {
                '=' => new Token(ETokenType.Equal, line, col, ""),
                '*' => new Token(ETokenType.Star, line, col, ""),
                '+' => new Token(ETokenType.Plus, line, col, ""),
                '-' => new Token(ETokenType.Minus, line, col, ""),
                '/' => new Token(ETokenType.Slash, line, col, ""),
                '%' => new Token(ETokenType.Modulo, line, col, ""),
                '^' => new Token(ETokenType.Power, line, col, ""),
                '!' => new Token(ETokenType.Not, line, col, ""),
                '>' => new Token(ETokenType.Greater, line, col, ""),
                '<' => new Token(ETokenType.Less, line, col, ""),
                ':' => new Token(ETokenType.Colon, line, col, ""),
                ';' => new Token(ETokenType.SemiColon, line, col, ""),
                '.' => new Token(ETokenType.Dot, line, col, ""),
                ',' => new Token(ETokenType.Comma, line, col, ""),
                '(' => new Token(ETokenType.OpenParen, line, col, ""),
                ')' => new Token(ETokenType.CloseParen, line, col, ""),
                '[' => new Token(ETokenType.OpenSquare, line, col, ""),
                ']' => new Token(ETokenType.CloseSquare, line, col, ""),
                '{' => new Token(ETokenType.OpenCurly, line, col, ""),
                '}' => new Token(ETokenType.CloseCurly, line, col, ""),
                _ => new Token(ETokenType.Error, line, col, $"Unexpected '{a}'")
            };
        }
    }

}
