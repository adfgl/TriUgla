using TriScript.Scanning;

namespace TriScript.Diagnostics
{
    public class DiagnosticBag
    {
        readonly List<Diagnostic> _items = new();

        public IReadOnlyList<Diagnostic> Items => _items;

        public void Report(ESeverity severity, string message, TextSpan span)
        {
            _items.Add(new Diagnostic(severity, message, span));
        }

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            _items.AddRange(diagnostics);
        }

        public bool HasErrors => _items.Any(d => d.severity == ESeverity.Error);

        public void Clear() => _items.Clear();

        public override string ToString()
        {
            if (_items.Count == 0)
                return "No diagnostics.";
            return string.Join(Environment.NewLine, _items);
        }
    }
}
