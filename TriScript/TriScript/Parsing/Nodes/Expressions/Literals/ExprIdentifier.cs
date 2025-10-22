using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprIdentifier : Expr
    {
        public ExprIdentifier(Token id) : base(id) { }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string id = source.GetString(Token.span);
            Variable var = stack.Current.Get(id);
            Value value = var.Value;
            return value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            string id = source.GetString(Token.span);

            if (!stack.Current.TryGet(id, out Variable var))
            {
                return EDataType.None;
            }
            return var.Value.type;
        }

        public override bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostic, out double si, out Dimension dim)
        {
            string name = src.GetString(Token.span);
            if (stack.Current.TryGet(name, out Variable v) && v.Units is not null)
            {
                si = v.Units.SiValue; 
                dim = v.Units.Dim; 
                return true;
            }

            var val = Evaluate(src, stack, heap);
            if (val.type.IsNumeric())
            { 
                si = val.AsDouble(); 
                dim = Dimension.None; 
                return true;
            }

            si = double.NaN; 
            dim = Dimension.None; 
            return false;
        }
    }
}
