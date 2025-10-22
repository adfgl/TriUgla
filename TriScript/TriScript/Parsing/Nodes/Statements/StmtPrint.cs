using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Data.Units;
using TriScript.Diagnostics;
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

            Expr expr = Args[0];
            var tmp = new DiagnosticBag();

            if (expr.EvaluateToSI(source, stack, heap, tmp, out double si, out Dimension dim))
            {
                UnitEval? u = expr.EvaluateToUnit(source, stack, heap);
                if (u.HasValue && u.Value.Dim.Equals(dim) && u.Value.ScaleToMeter > 0)
                    Console.WriteLine($"{si / u.Value.ScaleToMeter} [{u.Value}]");
                else
                    Console.WriteLine($"{si} [-]");
            }
            else
            {
                var v = expr.Evaluate(source, stack, heap);
                if (v.type.IsNumeric()) 
                    Console.WriteLine($"{v.AsDouble()} [-]");
                else if (v.type == EDataType.Pointer)
                    Console.WriteLine($"{heap.Get(v.pointer)} [-]");
                else 
                    Console.WriteLine($"{v} [-]");
            }
        }
    }
}
