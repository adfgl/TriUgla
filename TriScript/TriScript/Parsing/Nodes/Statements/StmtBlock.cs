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

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            stack.OpenScope();
            foreach (var node in Statements)
            {
                node.Evaluate(source, stack, heap);
            }
            stack.CloseScope();
        }
    }
}
