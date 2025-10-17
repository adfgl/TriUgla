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
            if (ch == '\"' || ch == '\'') return ReadString(ch);

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

            ETokenType type = ETokenType.Identifier;
            if (Keywords.Source.TryGetValue(text, out ETokenType keyword))
            {
                type = keyword;
                
            }
            else if (NativeFunctions.ALL.Has(text))
            {
                type = ETokenType.NativeFunction;
            }
            return new Token(type, line, col, text);
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

        Token ReadString(char quates)
        {
            int line = _line, col = _col;
            int startPos = _pos;     // at opening "
            Advance();               // consume opening "

            var sb = new System.Text.StringBuilder();

            while (true)
            {
                char c = Peek();
                if (c == EOF) break;            // unterminated -> return what we have
                if (c == quates) { Advance(); break; } // consume closing ", done
                if (c == '\n' || c == '\r')
                {   // stop at EOL for safety (treat as unterminated)
                    break;
                }

                if (c != '\\')                  // normal char
                {
                    sb.Append(c);
                    Advance();
                    continue;
                }

                Advance();                      // consume backslash
                char e = Peek();
                if (e == EOF) break;

                switch (e)
                {
                    case '"': sb.Append('"'); Advance(); break;
                    case '\\': sb.Append('\\'); Advance(); break;
                    case 'n': sb.Append('\n'); Advance(); break;
                    case 'r': sb.Append('\r'); Advance(); break;
                    case 't': sb.Append('\t'); Advance(); break;
                    case 'b': sb.Append('\b'); Advance(); break;
                    case 'f': sb.Append('\f'); Advance(); break;
                    case '0': sb.Append('\0'); Advance(); break;

                    case 'x':  // \xHH (2 hex)
                        {
                            Advance();
                            int v = 0, digits = 0;
                            for (int i = 0; i < 2; i++)
                            {
                                char h = Peek();
                                int d = HexVal(h);
                                if (d < 0) break;
                                v = (v << 4) | d;
                                Advance();
                                digits++;
                            }
                            sb.Append(digits > 0 ? (char)v : 'x'); // if bad, keep 'x' literally
                            break;
                        }

                    case 'u':  // \uHHHH (4 hex)
                        {
                            Advance();
                            int v = 0, digits = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char h = Peek();
                                int d = HexVal(h);
                                if (d < 0) break;
                                v = (v << 4) | d;
                                Advance();
                                digits++;
                            }
                            if (digits == 4)
                                sb.Append((char)v);
                            else
                                sb.Append('u'); // minimal recovery on malformed
                            break;
                        }

                    default:
                        sb.Append(e);
                        Advance();
                        break;
                }
            }

            string cooked = sb.ToString();
            return new Token(ETokenType.StringLiteral, line, col, cooked);
        }

        static int HexVal(char ch)
        {
            if (ch >= '0' && ch <= '9') return ch - '0';
            if (ch >= 'a' && ch <= 'f') return 10 + (ch - 'a');
            if (ch >= 'A' && ch <= 'F') return 10 + (ch - 'A');
            return -1;
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
                    return new Token(type, line, col, two);
                }
            }

            type = a switch
            {
                '~' => ETokenType.Tilda,
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
            return new Token(type, line, col, type == ETokenType.Error ? $"Unexpected '{a}'." : $"{a}");
        }
    }

}
