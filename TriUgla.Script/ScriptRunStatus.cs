using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script
{
    public enum ScriptRunStatus
    {
        Success,
        FailedToParse,
        FailedChecks,
        RuntimeError,
        Cancelled,
        Timeout,
        Exited
    }
}
