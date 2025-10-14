using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeBase
    {
        public NodeBase(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract TuValue Evaluate(TuStack stack);

        public static bool ValidIdentifier(string id, out string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                reason = "identifier must contain at least one character";
                return false;
            }

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
