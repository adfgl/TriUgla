using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeMacro : NodeBase
    {
        public NodeMacro(Token macro, NodeBase name, NodeBlock block, Token endMacro) : base(macro)
        {
            Name = name;
            Body = block;
            EndMacro = endMacro;
        }

        public Token Macro => Token;
        public NodeBase Name { get; }
        public NodeBlock Body { get; }

        public Token EndMacro { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue nameValue = Name.Evaluate(stack);   
            if (nameValue.type != EDataType.String)
            {
                throw new Exception("Expected string");
            }

            string name = nameValue.AsString();
            if (stack.Macros.ContainsKey(name))
            {
                throw new Exception($"Macro '{name}' already exists.");
            }
            stack.Macros[name] = Body;
            return TuValue.Nothing;
        }
    }
}
