
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprAssignment : Expr
    {
        public ExprAssignment(Token target, Expr value) : base(target)
        {
            Value = value;
        }

        public Expr Value { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string name = source.GetString(Token.span);
            if (!stack.Current.TryGet(name, out Variable var))
            {
                var = new Variable(name);
                stack.Current.Declare(var);
            }

            Value value = Value.Evaluate(source, stack, heap);
            var.Value = value;
            return value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return Value.PreviewType(source, stack, diagnostics);
        }
    }
}
