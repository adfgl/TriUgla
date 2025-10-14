using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeIdentifier : NodeBase
    {
        public NodeIdentifier(Token token) : base(token)
        {
            Name = token.value;
        }

        public string Name { get; private set; }
        public bool DeclareIfMissing { get; set; } = false;

        public override TuValue Evaluate(TuStack stack)
        {
            if (!ValidIdentifier(Name, out string reason))
            {
                throw new Exception(reason);
            }

            Variable? v;
            if (DeclareIfMissing)
            {
                v = stack.Current.GetOrDeclare(Name);
            }
            else
            {
                v = stack.Current.Get(Name);
            }

            if (v is null)
            {
                throw new Exception($"Undefined variable '{Name}'");
            }
            return v.Value;
        }
    }
}
