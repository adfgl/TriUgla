using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class StmtBlock : Stmt
    {
        public StmtBlock(List<Stmt> statements)
        {
            Statements = statements;
        }

        public IReadOnlyList<Stmt> Statements { get; }

        public override void Evaluate(Executor rt)
        {
            rt.OpenScope();
            foreach (var node in Statements)
            {
                node.Evaluate(rt);
            }
            rt.CloseScope();
        }
    }
}
