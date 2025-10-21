using TriScript.Data;

namespace TriScript.Parsing.Nodes
{
    public abstract class Expr 
    {
        public abstract Value Evaluate(Source source, ScopeStack stack, ObjHeap heap);
    }
}
