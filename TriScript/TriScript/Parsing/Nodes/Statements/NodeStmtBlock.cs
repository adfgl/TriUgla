using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class NodeStmtBlock : NodeStmtBase
    {
        public NodeStmtBlock(List<NodeStmtBase> statements)
        {
            Statements = statements;
        }

        public IReadOnlyList<NodeStmtBase> Statements { get; }

        public override Value Evaluate(Executor rt)
        {
            rt.OpenScope();
            foreach (NodeBase node in Statements)
            {
                node.Evaluate(rt);
            }
            rt.CloseScope();
            return Value.Nothing;
        }
    }
}
