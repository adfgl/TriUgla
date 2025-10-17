using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtTest : NodeStmtBase
    {
        public NodeStmtTest(Token test, NodeExprBase expr, NodeExprBase? message) : base(test)
        {
            TestExpression = expr;
            Message = message;
        }

        public NodeExprBase TestExpression { get; }
        public NodeExprBase? Message { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue test = TestExpression.Evaluate(rt);
            if (test.AsBoolean())
            {
                return test;
            }

            string error = "";
            if (Message is not null)
            {
                TuValue msg = Message.Evaluate(rt);
                error = msg.AsString();
            }

            throw new RunTimeException($"FAIL{(String.IsNullOrEmpty(error) ? "" : $": {error}")}", Token);
        }
    }
}
