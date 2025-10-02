using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Statements.Conditional
{
    public class NodeStmtIfElse : NodeStmtBase
    {
        public NodeExprBase Condition { get; }
        public NodeStmtBlock IfBody { get; }
        public List<NodeStmtElif> Elifs { get; } = new List<NodeStmtElif>();
        public NodeStmtBlock? ElseBody { get; }

        public NodeStmtIfElse(Token token, NodeExprBase condition, IEnumerable<NodeStmtBase> ifBlock, List<NodeStmtElif> elifs, IEnumerable<NodeStmtBase>? elseBody)
            : base(token)
        {
            Condition = condition;
            IfBody = new NodeStmtBlock(ifBlock);
            Elifs = elifs;
            ElseBody = elseBody is null ? null : new NodeStmtBlock(elseBody);
        }

        public override Value Evaluate(Scope scope)
        {
            if (Condition.Evaluate(scope).numeric > 0)
            {
                return IfBody.Evaluate(scope);
            }

            foreach (NodeStmtElif elif in Elifs)
            {
                if (elif.Condition.Evaluate(scope).numeric > 0)
                {
                    return elif.ElifBody.Evaluate(scope);
                }
            }

            if (ElseBody != null)
            {
                return ElseBody.Evaluate(scope);
            }
            return Value.Nothing;
        }
    }
}