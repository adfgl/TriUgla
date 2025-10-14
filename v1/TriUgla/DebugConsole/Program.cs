using TriUgla;
using TriUgla.Parsing;
using TriUgla.Parsing.Scanning;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string txt = File.ReadAllText(@"text.txt");

            Console.WriteLine(txt);
            Console.WriteLine();
            //new Scanner(txt).ReadAll();
            //Console.WriteLine();

            Executor executor = new Executor();

            Console.WriteLine("-----");
            Console.WriteLine(executor.Execute(txt)); ;

        }

    }
}
