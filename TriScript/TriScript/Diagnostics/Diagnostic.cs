using TriScript.Scanning;

namespace TriScript.Diagnostics
{
    public readonly struct Diagnostic
    {
        public readonly ESeverity severity;
        public readonly string message;   
        public readonly TextSpan span;    

        public Diagnostic(ESeverity severity, string message, TextSpan span)
        {
            this.severity = severity;
            this.message = message;
            this.span = span;
        }

        public override string ToString()
            => $"{severity}: {message} {span.start}..{span.start + span.length}";
    }
}
