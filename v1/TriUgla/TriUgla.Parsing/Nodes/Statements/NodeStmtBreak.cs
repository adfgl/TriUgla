using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtBreak : NodeStmtBase
    {
        public NodeStmtBreak(Token token) : base(token)
        {
        }

        protected override TuValue Eval(TuRuntime stack)
        {
            stack.Flow.SignalBreak();
            return TuValue.Nothing;
        }
    }
}
