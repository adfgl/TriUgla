using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Parsing;
using TriScript.Parsing.Nodes;

namespace TriScript
{
    public class Executor
    {
        readonly Source _source;

        public Executor(Source source)
        {
            _source = source;
        }

        public void Run()
        {
            DiagnosticBag diagnos = new DiagnosticBag();
            Parser parser = new Parser(_source, diagnos);
            ObjHeap heap = new ObjHeap();
            ScopeStack stack = new ScopeStack();
            TriProgram program = parser.Parse();

            foreach (Diagnostic item in diagnos.Items)
            {
                Console.WriteLine(item);
            }

            if (!diagnos.HasErrors)
            {
                program.Evaluate(_source, stack, heap);
            }
        }
    }
}
