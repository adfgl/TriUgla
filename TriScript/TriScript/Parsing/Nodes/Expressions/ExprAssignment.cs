
using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprAssignment : Expr
    {
        public ExprAssignment(Token target, Expr value)
        {
            Target = target;
            Value = value;
        }

        public Token Target { get; }
        public Expr Value { get; }

        public override Value Evaluate(Executor rt)
        {
            string name = rt.Source.GetString(Target.span);
            if (!rt.CurrentScope.TryGet(name, out Variable var))
            {
                var = new Variable(name);
                rt.CurrentScope.Declare(var);
            }

            Value value = Value.Evaluate(rt);
            var.Value = value;
            return value;
        }
    }
}
