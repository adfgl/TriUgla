namespace TriUgla.Script
{
    public sealed class Cursor(string source)
    {
        readonly string _source = source ?? string.Empty;

        int _pos;
        int _line;
        int _column;

        public int Index => _pos;
        public bool IsEnd => _pos >= _source.Length;
        public Position Position => new Position(_line, _column);
        public char Current => Peek();

        public char Peek(int offset = 0)
        {
            int index = _pos + offset;
            return (uint)index < (uint)_source.Length
                ? _source[index]
                : '\0';
        }

        public void Reset()
        {
            _pos = 0;
            _line = 0;
            _column = 0;
        }

        public void Advance()
        {
            if (IsEnd)
                return;

            char ch = _source[_pos];
            switch (ch)
            {
                case '\r':
                    if (Peek(1) == '\n')
                    {
                        _pos++;
                    }

                    _pos++;
                    _line++;
                    _column = 0;
                    return;

                case '\n':

                    _pos++;
                    _line++;
                    _column = 0;
                    return;

                default:

                    _pos++;
                    _column++;
                    return;
            }
        }

        public void Advance(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Advance();
            }
        }

        public bool Match(char ch)
        {
            if (Peek() != ch)
                return false;

            Advance();
            return true;
        }

        public bool Match(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (Peek(i) != text[i])
                {
                    return false;
                }
            }

            Advance(text.Length);
            return true;
        }

        public bool SkipWhiteSpace()
        {
            bool skipped = false;

            while (char.IsWhiteSpace(Peek()) &&
                   Peek() is not '\r' and not '\n')
            {
                skipped = true;
                Advance();
            }

            return skipped;
        }

        public bool SkipLineBreak()
        {
            if (Peek() is not '\r' and not '\n')
                return false;

            Advance();
            return true;
        }

        public string Text(Span span)
            => _source.Substring(span.Start, span.Length);

        public Span SpanFrom(int start)
            => new Span(start, _pos - start);
    }
}
