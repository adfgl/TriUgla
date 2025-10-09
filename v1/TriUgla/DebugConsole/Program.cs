using TriUgla;
using TriUgla.Parsing;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string txt = File.ReadAllText(@"text.txt");
            Console.WriteLine(txt);

            //Console.WriteLine();

            //new Scanner(txt).ReadAll();

            //Console.WriteLine();

            //Console.WriteLine(Executor.Run(txt)); ;

        }

    }
}
