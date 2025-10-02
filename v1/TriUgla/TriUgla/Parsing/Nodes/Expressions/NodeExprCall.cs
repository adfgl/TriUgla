namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprCall : NodeExprBase
    {
        public NodeExprBase Caller { get; }
        public NodeExprBase[] Arguments { get; }

        public NodeExprCall(Token token, NodeExprBase caller, NodeExprBase[] arguments) : base(token)
        {
            Caller = caller;
            Arguments = arguments;
        }

        public override Value Evaluate(Scope scope)
        {
            Value[] args = new Value[Arguments.Length];
            for (int i = 0; i < Arguments.Length; i++)
            {
                args[i] = Arguments[i].Evaluate(scope);
            }

            string id = Caller.Token.ToString();
            if (NativeFunctions.Functions.TryGetValue(id, out var native))
            {
                try
                {
                    return native.Function(args);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error invoking function '{id}': {ex.Message}", ex);
                }
            }
            throw new Exception($"Unknown function '{id}'");
        }
    }
}
