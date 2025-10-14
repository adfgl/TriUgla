using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TriUgla.Parsing.Scanning
{
    public sealed class TokenReader
    {
        const char EOF = '\0';
        readonly string _src;
        int _pos, _line, _col;

        public int Current => _pos;
        public int Line => _line;
        public int Column => _col;

        public TokenReader(string source) => _src = source ?? "";

        public Token Read()
        {
            SkipSpace();
            char ch = Peek();
            if (ch == EOF) return new Token(ETokenType.EOF, _line, _col);

            if (ch == '\n') return ReadNewline();
            if (ch == '/' && Peek(1) == '/') return ReadLineComment();
            if (ch == '/' && Peek(1) == '*') return ReadMultiLineComment();
            if (IsDigit(ch) || (ch == '.' && IsDigit(Peek(1)))) return ReadNumber();
            if (IsIdentStart(ch)) return ReadIdentifier();
            if (ch == '"') return ReadString();

            return ReadOperatorOrPunct();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        char Peek(int k = 0) => (_pos + k) < _src.Length ? _src[_pos + k] : EOF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        char Advance()
        {
            char c = _src[_pos++];
            if (c != '\n')
            {
                _col++;
            }
            else
            {
                _col = 0;
                _line++;
            }
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDigit(char c) => (uint)(c - '0') <= 9u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        void SkipSpace()
        {
            while (true)
            {
                char c = Peek();
                if (c == ' ' || c == '\t' || c == '\r') { Advance(); }
                else break;
            }
        }

        Token ReadNewline()
        {
            int line = _line, col = _col;
            Advance();
            return new Token(ETokenType.LineBreak, line, col);
        }

        Token ReadMultiLineComment()
        {
            int line = _line, col = _col;
            Advance(); Advance(); // '/*'

            int start = _pos;
            while (true)
            {
                char c = Peek();
                if (c == EOF)
                {
                    return new Token(ETokenType.Error, line, col, "Unterminated multi-line commnet.");
                }

                if (c == '*' && Peek(1) == '/')
                {
                    Advance();
                    Advance();
                    break;
                }
                Advance();
            }
            int len = _pos - start + 2;
            string text = _src.Substring(start, len - 2); // keep just the comment text (no //)
            return new Token(ETokenType.Comment, line, col, text);
        }

        Token ReadLineComment()
        {
            int line = _line, col = _col;
            Advance(); Advance(); // '//'
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

        Token ReadIdentifier()
        {
            int line = _line, col = _col, start = _pos;
            Advance();
            while (IsIdentPart(Peek())) Advance();
            int len = _pos - start;
            string text = _src.Substring(start, len);

            if (Keywords.Source.TryGetValue(text, out ETokenType keyword))
            {
                return new Token(keyword, line, col, string.Empty);
            }
            return new Token(ETokenType.IdentifierLiteral, line, col, text);
        }

        Token ReadNumber()
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
            return new Token(ETokenType.NumericLiteral, line, col, lexeme);
        }

        Token ReadString()
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

        Token ReadOperatorOrPunct()
        {
            int line = _line, col = _col;
            char a = Peek(), b = Peek(1);

            ETokenType type;
            if (b != EOF)
            {

                string two = $"{a}{b}";
                type = two switch
                {
                    "++" => ETokenType.PlusPlus,
                    "+=" => ETokenType.PlusEqual,
                    "-=" => ETokenType.MinusEqual,
                    "--" => ETokenType.MinusMinus,
                    "*=" => ETokenType.StarEqual,
                    "/=" => ETokenType.SlashEqual,
                    "%=" => ETokenType.ModuloEqual,
                    "!=" => ETokenType.NotEqual,
                    "==" => ETokenType.EqualEqual,
                    ">=" => ETokenType.GreaterOrEqual,
                    "<=" => ETokenType.LessOrEqual,
                    "&&" => ETokenType.And,
                    "||" => ETokenType.Or,
                    _ => ETokenType.Undefined,
                };
                if (type != ETokenType.Undefined)
                {
                    Advance(); Advance();
                    return new Token(type, line, col);
                }
            }

            Advance();

            type = a switch
            {
                '?' => ETokenType.Question,
                '#' => ETokenType.Hash,
                '=' => ETokenType.Equal,
                '*' => ETokenType.Star,
                '+' => ETokenType.Plus,
                '-' => ETokenType.Minus,
                '/' => ETokenType.Slash,
                '%' => ETokenType.Modulo,
                '^' => ETokenType.Power,
                '!' => ETokenType.Not,
                '>' => ETokenType.Greater,
                '<' => ETokenType.Less, 
                ':' => ETokenType.Colon, 
                ';' => ETokenType.SemiColon, 
                '.' => ETokenType.Dot, 
                ',' => ETokenType.Comma, 
                '(' => ETokenType.OpenParen,
                ')' => ETokenType.CloseParen,
                '[' => ETokenType.OpenSquare,
                ']' => ETokenType.CloseSquare,
                '{' => ETokenType.OpenCurly,
                '}' => ETokenType.CloseCurly,
                _ => ETokenType.Error
            };

            Advance();
            return new Token(type, line, col, type == ETokenType.Error ? $"Unexpected '{a}'." : String.Empty);
        }
    }

}
