using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Parsing;
using TriUgla.Script.Parsing.Nodes.Statements;

namespace TriUgla.Script
{
    public static class Script
    {
        public static void Run(Source source)
        {
            Diagnostics diagnostics = new Diagnostics();

            Parser parser = new Parser(source);

            StmtProg program = parser.Parse();
        }
    }
}
