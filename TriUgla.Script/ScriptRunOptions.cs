using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script
{
    public sealed class ScriptRunOptions
    {
        public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(5);

        public bool RunWithWarnings { get; init; } = true;

        public bool RunWithErrors { get; init; } = false;
    }
}
