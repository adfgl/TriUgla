using TriScript.Data;
using TriScript.Parsing;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string content = File.ReadAllText(@"test.txt");

            Console.WriteLine(content);
            Console.WriteLine("\n=================\n");

            Source src = new Source(content);

            ScopeStack scope = new ScopeStack();
            Diagnostics diagnostics = new Diagnostics();
            Parser parser = new Parser(src, scope, diagnostics);

            VisitorEval eval = new VisitorEval(scope, src, diagnostics);
            VisitorUnit type = new VisitorUnit(scope, src, diagnostics);
            VisitorUnit unit = new VisitorUnit(scope, src, diagnostics);

            StmtProgram program = parser.Parse();
            program.Accept(type, out _);
            program.Accept(unit, out _);

            if (!diagnostics.HasErrors)
            {
                program.Accept(eval, out _);
            }

            foreach (Diagnostic item in diagnostics.Items)
            {
                Console.WriteLine(item);
            }
        }
    }
}
