using TriScript;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Source source = new Source(File.ReadAllText(@"test.txt"));
        }
    }
}
