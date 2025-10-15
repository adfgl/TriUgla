using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeStmtMacro : NodeBase
    {
        public NodeStmtMacro(Token macro, NodeBase name, NodeStmtBlock block, Token endMacro) : base(macro)
        {
            Name = name;
            Body = block;
            EndMacro = endMacro;
        }

        public Token Macro => Token;
        public NodeBase Name { get; }
        public NodeStmtBlock Body { get; }

        public Token EndMacro { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue nameValue = Name.Evaluate(stack);   
            if (nameValue.type != EDataType.String)
            {
                throw new Exception("Expected string");
            }

            string name = nameValue.AsString();
            if (stack.Current.Macros.ContainsKey(name))
            {
                throw new Exception($"Macro '{name}' already exists.");
            }
            stack.Current.Macros[name] = Body;
            return TuValue.Nothing;
        }
    }
}
