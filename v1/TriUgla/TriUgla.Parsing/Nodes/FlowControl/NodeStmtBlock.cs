using System.Collections.Generic;
using System.Reflection.Emit;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtBlock : NodeBase
    {
        public NodeStmtBlock(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToArray();
        }

        public IReadOnlyList<NodeBase> Statements { get; }


        public override TuValue Evaluate(TuRuntime stack)
        {
            TuValue last = TuValue.Nothing;

            RuntimeFlow flow = stack.Flow;
            foreach (NodeBase node in Statements)
            {
                if (!stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;

                var v = node.Evaluate(stack);
                if (v.type != EDataType.Nothing) last = v;

                if (!stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;
            }
            return TuValue.Nothing;
        }
    }
}
