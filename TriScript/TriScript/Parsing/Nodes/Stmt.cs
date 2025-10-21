using TriScript.Data;

namespace TriScript.Parsing.Nodes
{
    public abstract class Stmt
    {
        public abstract void Evaluate(Source source, ScopeStack stack, ObjHeap heap);
    }
}
