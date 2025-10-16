using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtBreak : NodeStmtBase
    {
        public NodeStmtBreak(Token token) : base(token)
        {
        }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            rt.Flow.SignalBreak();
            return TuValue.Nothing;
        }
    }
}
