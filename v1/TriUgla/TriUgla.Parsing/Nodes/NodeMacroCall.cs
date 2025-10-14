using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeMacroCall : NodeBase
    {
        public NodeMacroCall(Token token, NodeBase name) : base(token)
        {
            Name = name;
        }

        public NodeBase Name { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue nameValue = Name.Evaluate(stack);
            if (nameValue.type != EDataType.String)
            {
                throw new Exception("Expected string");
            }

            string name = nameValue.AsString();
            if (!stack.Current.Macros.TryGetValue(name, out var body))
            {
                throw new Exception($"Call: macro '{name}' not defined.");
            }
            body.Evaluate(stack);
            return TuValue.Nothing;
        }
    }
}
