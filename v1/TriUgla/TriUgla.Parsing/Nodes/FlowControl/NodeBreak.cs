using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeBreak : NodeBase
    {
        public NodeBreak(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            stack.Flow.SignalBreak();
            return TuValue.Nothing;
        }
    }
}
