using System.Runtime.CompilerServices;
using TriScript.Diagnostics;

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

        public void Reset()
        {
            _pos = _line = _col = 0;
        }

        public Token Read(DiagnosticBag? diagnostic)
        {
            SkipSpace();
            char ch = Peek();

            if (ch == EOF) return ReadEndOfFile();
            if (ch == '\n') return ReadNewline();
            if (IsDigit(ch) || (ch == '.' && IsDigit(Peek(1)))) return ReadNumber();
            if (IsIdentStart(ch)) return ReadIdentifier();
            return ReadUnknown(diagnostic);
        }

        Token ReadUnknown(DiagnosticBag? diagnostic)
        {
            int startPos = _pos;
            int line = _line, col = _col;
            char bad = Advance();

            TextSpan span = new TextSpan(startPos, 1);
            diagnostic?.Report(ESeverity.Error, $"Unexpected character '{bad}'.", span);
            return new Token(ETokenType.Undefined, line, col, span);
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
            return new Token(ETokenType.LiteralNemeric, line, col, span);
        }

        Token ReadIdentifier()
        {
            int line = _line, col = _col, start = _pos;
            if (!IsIdentStart(Advance())) throw new Exception();
            while (IsIdentPart(Peek())) Advance();
            TextSpan span = new TextSpan(start, _pos - start);
            return new Token(ETokenType.LiteralId, line, col, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDigit(char c) => (uint)(c - '0') <= 9u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        Token ReadEndOfFile()
        {
            Token tkn = new Token(ETokenType.EndOfFile, _line, _col, new TextSpan(_pos, 0));
            return tkn;
        }

        Token ReadNewline()
        {
            Token tkn = new Token(ETokenType.LineBreak, _line, _col, new TextSpan(_pos, 0));
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
