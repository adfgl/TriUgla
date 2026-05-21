using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Parsing.Execution
{
    public sealed class ScriptExitException(int code) : Exception
    {
        public int Code { get; } = code;
    }

    public sealed class ScriptBreakException : Exception;

    public sealed class ScriptContinueException : Exception;
}
