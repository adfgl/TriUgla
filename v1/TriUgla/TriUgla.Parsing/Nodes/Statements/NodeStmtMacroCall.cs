using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtMacroCall : NodeStmtBase
    {
        public NodeStmtMacroCall(Token token, NodeExprBase name) : base(token)
        {
            Name = name;
        }

        public NodeExprBase Name { get; }

        protected override TuValue Eval(TuRuntime stack)
        {
            TuValue nameValue = Name.Evaluate(stack);
            if (nameValue.type != EDataType.Text)
            {
                throw new RunTimeException(
                    $"Call expects a string macro name, but the expression evaluated to '{nameValue.type}'.",
                    Name.Token);
            }

            string macroName = nameValue.AsString();
            if (string.IsNullOrEmpty(macroName.Trim()))
            {
                throw new CompileTimeException(
                    "Macro name cannot be empty.",
                    Name.Token);
            }

            if (!stack.Current.Macros.TryGetValue(macroName, out var body))
            {
                throw new CompileTimeException(
                    $"Macro '{macroName}' is not defined.",
                    Token);
            }
            return body.Evaluate(stack);
        }
    }
}
