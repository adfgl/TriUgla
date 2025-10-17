namespace TriScript.Scanning
{
    public class Source
    {
        readonly string _content;
        readonly Dictionary<TextSpan, string> _cache = new Dictionary<TextSpan, string>();

        public Source(string source)
        {
            _content = source;
        }

        public string Content => _content;

        public string GetString(TextSpan span)
        {
            if (_cache.TryGetValue(span, out var cached))
            {
                return cached;
            }

            int start = span.start;
            int len = span.length;
            int max = _content.Length;

            if ((uint)start >= (uint)max)
                return string.Empty;
            if (start + len > max)
                len = max - start;
            if (len <= 0)
                return string.Empty;

            string result = _content.Substring(start, len);
            _cache[span] = result;
            return result;
        }

        public override string ToString() => _content;
    }
}
