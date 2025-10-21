using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class StmtProgram : Stmt
    {
        public StmtProgram(StmtBlock body)
        {
            Body = body;
        }

        public StmtBlock Body { get; }

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            Body.Evaluate(source, stack, heap);
        }
    }
}
