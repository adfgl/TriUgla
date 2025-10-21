using TriScript.Data;
using TriScript.Parsing.Nodes.Statements;

namespace TriScript.Parsing.Nodes
{
    public class TriProgram : Stmt
    {
        public TriProgram(StmtBlock block)
        {
            Block = block;
        }

        public StmtBlock Block { get; }

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            Block.Evaluate(source, stack, heap);
        }
    }
}
