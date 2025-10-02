using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtBlock : NodeStmtBase
    {
        public NodeStmtBase[] Statements { get; }

        public NodeStmtBlock(IEnumerable<NodeStmtBase> statements) : base(default)
        {
            Statements = statements.ToArray();
        }

        public override Value Evaluate(Scope scope)
        {
            Value value = Value.Nothing;
            foreach (NodeStmtBase stmt in Statements)
            {
                value = stmt.Evaluate(scope);
            }
            return value;
        }
    }
}
