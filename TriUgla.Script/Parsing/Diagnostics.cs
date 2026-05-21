using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class Diagnostics
    {
        readonly List<Diagnostic> _items = [];

        public IReadOnlyList<Diagnostic> Items => _items;

        public bool HasErrors =>
            _items.Any(x => x.Severity == Severity.Error);

        public void Report(
            Severity severity,
            string message,
            Token token)
        {
            string line =
                token.Source.GetLineText(token.Position.Line);

            string marker =
                new string(' ', token.Position.Column) +
                new string('^', Math.Max(1, token.Span.Length));

            _items.Add(new Diagnostic(
                severity,
                message,
                token.Position,
                token.Span,
                line,
                marker));
        }

        public void Error(string message, Token token)
        {
            Report(Severity.Error, message, token);
        }

        public void Warning(string message, Token token)
        {
            Report(Severity.Warning, message, token);
        }

        public void Info(string message, Token token)
        {
            Report(Severity.Info, message, token);
        }

        public override string ToString()
        {
            return string.Join(
                Environment.NewLine + Environment.NewLine,
                _items.Select(x => x.ToString()));
        }
    }
}
