using TriScript.Scanning;

namespace TriScript.Parsing
{
    public readonly struct Diagnostic
    {
        public readonly ESeverity severity;
        public readonly string message;
        public readonly TextPosition position;
        public readonly TextSpan span;

        public Diagnostic(ESeverity severity, string message, TextPosition position, TextSpan span)
        {
            this.severity = severity;
            this.message = message;
            this.position = position;
            this.span = span;
        }

        public override string ToString()
            => $"{severity}: {message} at {position} [{position}]";
    }
}
