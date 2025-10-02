using System.Diagnostics;
using TriUgla.Parsing;

namespace TriUgla
{
    public static class Executor
    {
        public static Value Run(string source)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Scope global = new Scope();
            Value result = Value.Nothing;

            result = new Parser(source).Parse().Evaluate(global);

            stopwatch.Stop();
            Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");

            return result;
        }
    }
}
