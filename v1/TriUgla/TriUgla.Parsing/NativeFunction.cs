using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class NativeFunction
    {
        public string Name { get; }
        public string Description { get; }
        public int MinArgs { get; set; }
        public int MaxArgs { get; set; }
        public Func<Value[], Value> Function { get; }

        public NativeFunction(string name, string description, Func<Value[], Value> function)
        {
            Name = name;
            Description = description;
            Function = function;
        }

        public override string ToString() => $"{Name} — {Description}";
    }
}
