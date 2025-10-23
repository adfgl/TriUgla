namespace TriScript.Scanning
{
    public sealed class Source
    {
        private readonly string _content;
        private readonly Dictionary<TextSpan, string> _cache = new Dictionary<TextSpan, string>();

        public Source(string source)
        {
            _content = source ?? string.Empty;
        }

        public string Content => _content;
        public int Length => _content.Length;

        public ReadOnlySpan<char> Slice(TextSpan span)
        {
            int start = span.start;
            int len = span.length;
            int max = _content.Length;

            if ((uint)start >= (uint)max)
                return ReadOnlySpan<char>.Empty;
            if (start + len > max)
                len = max - start;
            if (len <= 0)
                return ReadOnlySpan<char>.Empty;

            return _content.AsSpan(start, len);
        }

        public string GetString(TextSpan span)
        {
            if (_cache.TryGetValue(span, out var cached))
                return cached;

            ReadOnlySpan<char> slice = Slice(span);
            if (slice.IsEmpty)
            {
                return string.Empty;
            }

            string result = new string(slice);
            _cache[span] = result;
            return result;
        }

        public override string ToString() => _content;
    }
}
