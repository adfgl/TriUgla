using TriScript.Data;
using TriScript.Parsing.Nodes.Statements;

namespace TriScript.Parsing.Nodes
{
    public class TriProgram : Stmt
    {
        public TriProgram(List<Stmt> block)
        {
            Block = block;
        }

        public IReadOnlyList<Stmt> Block { get; }

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            foreach (Stmt stmt in Block)
            {
                stmt.Evaluate(source, stack, heap);
            }
        }
    }
}
