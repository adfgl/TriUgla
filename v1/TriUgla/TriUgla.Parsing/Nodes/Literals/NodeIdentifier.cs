using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeIdentifier : NodeBase
    {
        public NodeIdentifier(Token token, NodeBase? index) : base(token)
        {
            Index = index;
            Name = token.value;
        }

        public string Name { get; private set; }
        public NodeBase? Index { get; }
        public bool DeclareIfMissing { get; set; } = false;

        public override TuValue Evaluate(TuStack stack)
        {
            if (Index is not null)
            {
                TuValue numeric = Index.Evaluate(stack);
                if (numeric.type != EDataType.Numeric)
                {
                    throw new Exception();
                }

                double dbl = numeric.AsNumeric();
                if (dbl % 1 != 0)
                {
                    throw new Exception("has fraction");
                }

                int i = (int)dbl;

                Name = $"{Name}({i})";
            }

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

        public static bool ValidIdentifier(string id, out string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                reason = "identifier must contain at least one character";
                return false;
            }

            // verbatim?
            bool verbatim = id[0] == '@';
            int i = verbatim ? 1 : 0;

            if (i >= id.Length)
            {
                reason = "verbatim identifier '@' must be followed by a name";
                return false;
            }

            // first char after optional '@'
            char c0 = id[i];
            if (!(char.IsLetter(c0) || c0 == '_'))
            {
                reason = "identifier must start with a letter or underscore";
                return false;
            }

            // remaining chars
            for (int j = i + 1; j < id.Length; j++)
            {
                char cj = id[j];
                if (!(char.IsLetterOrDigit(cj) || cj == '_'))
                {
                    reason = $"invalid character '{cj}' in identifier";
                    return false;
                }
            }

            // keywords (unless verbatim)
            if (!verbatim && Keywords.Source.ContainsKey(id))
            {
                reason = "identifier is a reserved C# keyword; use @keyword to escape";
                return false;
            }

            reason = "";
            return true;

        }
    }
}
