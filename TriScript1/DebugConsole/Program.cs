using TriScript.Data;
using TriScript.Parsing;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace DebugConsole
{
    internal class Program
    {

        private static readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);

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
            VisitorType type = new VisitorType(scope, src, diagnostics);

            StmtProgram program = parser.Parse();
            program.Accept(type, out _);

            if (!diagnostics.HasErrors)
            {
                program.Accept(eval, out _);
            }

            foreach (Diagnostic item in diagnostics.Items)
            {
                Console.WriteLine(item);
            }

            //string path = args.Length > 0 ? args[0] : "C:\\Users\\zhukopav\\Documents\\github\\test.txt";
            ////Console.CancelKeyPress += (_, e) => { e.Cancel = true; _stop.Signal(); };

            //using var runner = new LiveRunner(path);
            //runner.Start();

            //Console.WriteLine($"Watching: {path}");
            //Console.WriteLine("Edit & save the file to re-run. Press Ctrl+C to exit.");

            //_stop.Wait();
        }

    }
}
