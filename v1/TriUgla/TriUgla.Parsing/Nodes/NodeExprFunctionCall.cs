using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeExprFunctionCall : NodeBase
    {
        public NodeExprFunctionCall(Token name, NodeExprIdentifier id, IEnumerable<NodeBase> args) : base(name)
        {
            Id = id;
            Args = args.ToArray();
        }

        public NodeExprIdentifier Id { get; }
        public IReadOnlyList<NodeBase> Args { get; }

        public override string ToString()
        {
            return $"{Token.value}({Args.Count})";
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            string name = Id.Name;

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
                throw new CompileTimeException($"Function '{name}' not supported.", Id.Token);
            }

            if (!fun.TryExecute(args, out TuValue result, out string error))
            {
                if (allCompileTimeKnown)
                {
                    throw new CompileTimeException(error, Id.Token);
                }
                else
                {
                    throw new RunTimeException(error, Id.Token);
                }
            }
            return result;
        }
    }
}
