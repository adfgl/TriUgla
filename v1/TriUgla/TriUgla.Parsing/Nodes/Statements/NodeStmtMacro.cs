using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtMacro : NodeStmtBase
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

        public override TuValue Evaluate(TuRuntime stack)
        {
            TuValue nameValue = Name.Evaluate(stack);
            if (nameValue.type != EDataType.String)
            {
                if (Name is NodeExprIdentifier id)
                {
                    throw new CompileTimeException(
                        $"Macro definition expects a string name: variable '{id.Name}' has type '{nameValue.type}'.",
                        Name.Token);
                }

                throw new RunTimeException(
                    $"Macro definition expects a string name, but the expression evaluated to '{nameValue.type}'.",
                    Name.Token);
            }

            string macroName = nameValue.AsString();
            if (string.IsNullOrEmpty(macroName.Trim()))
            {
                throw new CompileTimeException(
                    "Macro name cannot be empty.",
                    Name.Token);
            }

            if (stack.Current.Macros.ContainsKey(macroName))
            {
                throw new CompileTimeException(
                    $"Macro '{macroName}' is already defined.",
                    Token);
            }
            stack.Current.Macros[macroName] = Body;
            return TuValue.Nothing;
        }
    }
}
