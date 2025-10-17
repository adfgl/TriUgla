using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
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

        public abstract TuValue Evaluate(TuRuntime rt);

        protected abstract TuValue EvaluateInvariant(TuRuntime rt);

        public static string ToOrdinal(int n)
        {
            int mod100 = n % 100;
            if (mod100 is >= 11 and <= 13) return n + "th";
            return (n % 10) switch
            {
                1 => n + "st",
                2 => n + "nd",
                3 => n + "rd",
                _ => n + "th"
            };
        }

        public static void CheckDivisionByZero(NodeBase node, TuValue value)
        {
            if (value.type != EDataType.Float)
            {
                throw new Exception("Node must be evaluated to numeric at this point.");
            }

            if (value.AsNumeric() != 0) return;

            if (node is NodeExprLiteralBase)
            {
                throw CompileTimeException.DivisionByZero(node.Token);
            }
            else
            {
                throw RunTimeException.DivisionByZero(node.Token);
            }
        }

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
