using TriScript.Scanning;

namespace TriScript.Parsing
{
    public sealed class Diagnostics
    {
        readonly List<Diagnostic> _items = new();

        public IReadOnlyList<Diagnostic> Items => _items;

        public void Report(Source source, ESeverity severity, string message, TextPosition pos, TextSpan span)
        {
            _items.Add(new Diagnostic(source, severity, message, pos, span));
        }

        public void Report(Source source, ESeverity severity, string message, Token token)
        {
            _items.Add(new Diagnostic(source, severity, message, token.position, token.span));
        }

        public bool HasErrors
        {
            get
            {
                return _items.Any(d => d.severity == ESeverity.Error);
            }
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
