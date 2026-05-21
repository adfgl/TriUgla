using TriUgla.Script.Scanning;

namespace TriUgla.Script
{
    public sealed class Source
    {
        readonly string _text;

        public Source(string? text)
        {
            _text = text ?? string.Empty;
        }

        public int Length => _text.Length;

        public bool IsEmpty => _text.Length == 0;

        public char this[int index]
        {
            get
            {
                return (uint)index < (uint)_text.Length
                    ? _text[index]
                    : '\0';
            }
        }

        public string Slice(Span span)
        {
            if (span.Start < 0 || span.Start >= _text.Length)
                return string.Empty;

            int length = Math.Clamp(
                span.Length,
                0,
                _text.Length - span.Start);

            return _text.Substring(span.Start, length);
        }

        public string GetLineText(int line)
        {
            if (line < 0)
                return string.Empty;

            if (!TryGetLineBounds(line, out int start, out int length))
                return string.Empty;

            return _text.Substring(start, length);
        }

        public bool TryGetLineBounds(
            int targetLine,
            out int start,
            out int length)
        {
            start = 0;
            length = 0;

            if (targetLine < 0)
                return false;

            int currentLine = 0;
            int lineStart = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                char ch = _text[i];

                if (ch is '\r' or '\n')
                {
                    if (currentLine == targetLine)
                    {
                        start = lineStart;
                        length = i - lineStart;
                        return true;
                    }

                    if (ch == '\r' &&
                        i + 1 < _text.Length &&
                        _text[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentLine++;
                    lineStart = i + 1;
                }
            }

            if (currentLine == targetLine)
            {
                start = lineStart;
                length = _text.Length - lineStart;
                return true;
            }

            return false;
        }

        public string MakeMarker(Position position, Span span)
        {
            string line = GetLineText(position.Line);

            int column = Math.Clamp(position.Column, 0, line.Length);

            int length = Math.Max(1, span.Length);

            if (column + length > line.Length)
                length = Math.Max(1, line.Length - column);

            string prefix = line[..column];

            string markerPrefix = new(
                prefix.Select(ch => ch == '\t' ? '\t' : ' ').ToArray());

            return markerPrefix + new string('^', length);
        }

        public override string ToString()
            => _text;
    }
}
