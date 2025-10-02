using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Nodes.Statements;

namespace TriUgla.Parsing.Nodes
{
    public sealed class ProgramNode : NodeBase
    {
        public NodeStmtBlock Block { get; }

        public ProgramNode(List<NodeStmtBase> statements) : base(default)
        {
            Block = new NodeStmtBlock(statements);
        }

        public override Value Evaluate(Scope scope)
        {
            return Block.Evaluate(scope);
        }
    }
}
