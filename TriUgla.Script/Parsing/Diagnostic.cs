using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class Diagnostic(
      Severity severity,
      string message,
      Position position,
      Span span,
      string lineText,
      string marker)
    {
        public Severity Severity { get; } = severity;
        public string Message { get; } = message;
        public Position Position { get; } = position;
        public Span Span { get; } = span;
        public string LineText { get; } = lineText;
        public string Marker { get; } = marker;

        public override string ToString()
        {
            return
                $"{Severity}: {Message}\n" +
                $"  at {Position}\n" +
                $"  {LineText}\n" +
                $"  {Marker}";
        }
    }

}
