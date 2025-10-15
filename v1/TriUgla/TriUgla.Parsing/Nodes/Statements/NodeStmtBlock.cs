using System.Collections.Generic;
using System.Reflection.Emit;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtBlock : NodeStmtBase
    {
        public NodeStmtBlock(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToArray();
        }

        public IReadOnlyList<NodeBase> Statements { get; }

        protected override TuValue Evaluate(TuRuntime stack)
        {
            TuValue last = TuValue.Nothing;

            RuntimeFlow flow = stack.Flow;
            foreach (NodeBase node in Statements)
            {
                if (!stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;

                var v = node.Eval(stack);
                if (v.type != EDataType.Nothing) last = v;

                if (!stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;
            }
            return last;
        }
    }
}
