using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeExprFunctionCall : NodeBase
    {
        public NodeExprFunctionCall(Token name, IEnumerable<NodeBase> args) : base(name)
        {
            Args = args.ToArray();
        }

        public IReadOnlyList<NodeBase> Args { get; }

        public override string ToString()
        {
            return $"{Token.value}({Args.Count})";
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            string name = Token.value;

            bool allCompileTimeKnown = true;
            TuValue[] args = new TuValue[Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                NodeBase arg = Args[i];
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
