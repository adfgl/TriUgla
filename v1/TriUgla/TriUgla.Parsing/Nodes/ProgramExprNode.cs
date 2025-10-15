using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class ProgramExprNode : NodeBase
    {
        public ProgramExprNode(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToList();
        }

        public IReadOnlyList<NodeBase> Statements { get; }

        public override TuValue Evaluate(TuRuntime stack)
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
