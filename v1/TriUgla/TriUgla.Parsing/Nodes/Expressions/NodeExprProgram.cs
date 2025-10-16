using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprProgram : NodeExprBase
    {
        public NodeExprProgram(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToList();
        }

        public IReadOnlyList<NodeBase> Statements { get; }

        protected override TuValue Eval(TuRuntime stack)
        {
            stack.OpenScope();

            stack.Global.Declare(new Token(ETokenType.IdentifierLiteral, -1, -1, "Pi"), new TuValue(3.1415926535897932));

            TuValue result = TuValue.Nothing;
            foreach (NodeBase item in Statements)
            {
                TuValue value = item.Evaluate(stack);
                if (value.type != EDataType.Nothing)
                {
                    result = value;
                }
            }
            return result;
        }
    }
}
