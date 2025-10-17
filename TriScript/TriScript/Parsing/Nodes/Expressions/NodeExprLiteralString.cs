using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class NodeExprLiteralString : NodeExprBase
    {
        public NodeExprLiteralString(Token value)
        {
            Token = value;
        }

        public Token Token { get; }

        public override Value Evaluate(Executor rt)
        {
            string content = rt.Source.GetString(Token.span);
            ObjString str = new ObjString(content);
            Pointer ptr = rt.Heap.Allocate(str);
            Value value = new Value(ptr);
            return value;
        }
    }
}
