using TriUgla.Script.Data;

namespace TriUgla.Script.Parsing
{
    public sealed class ExecutionResult
    {
        public Value Value { get; set; } = Value.Undefined;

        public int ExitCode { get; set; }
        public bool HasExited { get; set; }

        public Exception? Exception { get; set; }

        public ScriptContext Context { get; } = new();

        public IReadOnlyList<string> Log => Context.Log;
    }
}
