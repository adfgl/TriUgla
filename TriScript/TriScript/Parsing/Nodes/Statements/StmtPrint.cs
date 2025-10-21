using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Objects;

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
            }
            else
            {
                Value value = Args[0].Evaluate(source, stack, heap);
                if (value.type != EDataType.Pointer)
                {
                    Console.WriteLine(value);
                }
                else
                {
                    Obj obj = heap.Get(value.pointer);
                    Console.WriteLine(obj);
                }
            }
        }
    }
}
