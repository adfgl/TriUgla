using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Parsing.Nodes.Expressions;
using TriScript.Parsing.Nodes.Expressions.Literals;

namespace TriScript.Parsing.Nodes.Statements
{
    public sealed class StmtPrint : Stmt
    {
        public StmtPrint(List<Expr> args)
        {
            Args = args;
        }

        public List<Expr> Args { get; }

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            if (Args.Count == 0)
            {
                Console.WriteLine();
                return;
            }

            // For now, print only the first argument (you can loop if needed)
            Expr expr = Args[0];
            Value value = expr.Evaluate(source, stack, heap);

            // Try to detect if this expression refers to a named variable with units
            string? unitLabel = null;

            if (expr is ExprIdentifier id)
            {
                string name = source.GetString(id.Token.span);
                if (stack.Current.TryGet(name, out Variable var) && var.Units is not null)
                {
                    unitLabel = $"[{var.Units.Preferred.ToString()}]";
                }
            }
            else if (expr is ExprWithUnit wu)
            {
                // explicit cast, use that
                unitLabel = $"[{wu.Units.ToString()}]";
            }

            // Fallback if nothing attached
            if (unitLabel == null)
                unitLabel = "[-]";

            // If numeric → print with unit tag
            if (value.type.IsNumeric())
            {
                Console.WriteLine($"{value.AsDouble()} {unitLabel}");
                return;
            }

            // If pointer → object print
            if (value.type == EDataType.Pointer)
            {
                Obj obj = heap.Get(value.pointer);
                Console.WriteLine($"{obj} {unitLabel}");
                return;
            }

            // Anything else
            Console.WriteLine($"{value} {unitLabel}");
        }
    }
}
