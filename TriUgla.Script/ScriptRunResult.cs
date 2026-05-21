using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Parsing;

namespace TriUgla.Script
{
    public sealed class ScriptRunResult
    {
        public ScriptRunStatus Status { get; set; }

        public Source Source { get; init; } = new("");

        public Diagnostics Diagnostics { get; init; } = new();

        public StmtProg? Program { get; set; }

        public ExecutionResult Execution { get; init; } = new();

        public ScriptContext Context => Execution.Context;

        public IReadOnlyList<string> Log => Execution.Log;

        public bool Success => Status is ScriptRunStatus.Success or ScriptRunStatus.Exited;
    }
}
