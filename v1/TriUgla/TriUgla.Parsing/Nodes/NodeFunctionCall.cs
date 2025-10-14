using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeFunctionCall : NodeBase
    {
        public NodeFunctionCall(Token name, NodeIdentifier id, IEnumerable<NodeBase> args) : base(name)
        {
            Id = id;
            Args = args.ToArray();
        }

        public NodeIdentifier Id { get; }
        public IReadOnlyList<NodeBase> Args { get; }

        public override string ToString()
        {
            return $"{Token.value}({Args.Count})";
        }

        public override TuValue Evaluate(TuStack stack)
        {
            string function = Id.Name;

            TuValue[] args = new TuValue[Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = Args[i].Evaluate(stack);
            }

            return function switch
            {
                "Acos" => NativeFunctions.Acos(args),
                "Asin" => NativeFunctions.Asin(args),
                "Atan" => NativeFunctions.Atan(args),
                "Atan2" => NativeFunctions.Atan2(args),
                "Ceil" => NativeFunctions.Ceil(args),
                "Cos" => NativeFunctions.Cos(args),
                "Cosh" => NativeFunctions.Cosh(args),
                "Exp" => NativeFunctions.Exp(args),
                "Fabs" => NativeFunctions.Fabs(args),
                "Fmod" => NativeFunctions.Fmod(args),
                "Floor" => NativeFunctions.Floor(args),
                "Hypot" => NativeFunctions.Hypot(args),
                "Log" => NativeFunctions.Log(args),
                "Log10" => NativeFunctions.Log10(args),
                "Max" => NativeFunctions.Max(args),
                "Min" => NativeFunctions.Min(args),
                "Modulo" => NativeFunctions.Modulo(args),
                "Rand" => NativeFunctions.Rand(args),
                "Round" => NativeFunctions.Round(args),
                "Sqrt" => NativeFunctions.Sqrt(args),
                "Sin" => NativeFunctions.Sin(args),
                "Sinh" => NativeFunctions.Sinh(args),
                "Tan" => NativeFunctions.Tan(args),
                "Tanh" => NativeFunctions.Tanh(args),
                "Print" => NativeFunctions.Print(args),
                _ => throw new Exception($"Unknown function '{function}'."),
            };
        }
    }
}
