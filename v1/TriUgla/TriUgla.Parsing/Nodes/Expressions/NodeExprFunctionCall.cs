using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprFunctionCall : NodeExprBase
    {
        public NodeExprFunctionCall(Token name, IEnumerable<NodeExprBase> args) : base(name)
        {
            Args = args.ToArray();
        }

        public IReadOnlyList<NodeExprBase> Args { get; }

        public override string ToString()
        {
            return $"{Token.value}({Args.Count})";
        }

        protected override TuValue Eval(TuRuntime stack)
        {
            string name = Token.value;

            bool allCompileTimeKnown = true;
            TuValue[] args = new TuValue[Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                NodeExprBase arg = Args[i];
                if (allCompileTimeKnown && arg is not NodeExprLiteralBase)
                {
                    allCompileTimeKnown = false;
                }

                args[i] = arg.Evaluate(stack);
            }

            if (!stack.Functions.TryGet(name, out NativeFunction fun))
            {
                throw new CompileTimeException($"Function '{name}' not supported.", Token);
            }

            if (!fun.TryExecute(args, out TuValue result, out string error))
            {
                if (allCompileTimeKnown)
                {
                    throw new CompileTimeException(error, Token);
                }
                else
                {
                    throw new RunTimeException(error, Token);
                }
            }
            return result;
        }
    }
}
