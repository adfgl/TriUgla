using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class ExprTypePreviewVisitor : IExprVisitor<EDataType>
    {
        public ExprTypePreviewVisitor(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            ScopeStack = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack ScopeStack { get; set; }
        public Source Source { get; set; }
        public Diagnostics Diagnostics { get; set; }

        public EDataType Visit(ExprNumeric node)
        {
            return node.Value.type;
        }

        public EDataType Visit(ExprAssignment node)
        {
            return node.Value.Accept(this);
        }

        public EDataType Visit(ExprBinary node)
        {
            EDataType l = node.Left.Accept(this);
            EDataType r = node.Right.Accept(this);

            if (l == r) return l;
            if (l.IsNumeric() && r.IsNumeric())
            {
                return EDataType.Real;
            }

            string error;
            switch (node.Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                case ETokenType.Star:
                case ETokenType.Slash:
                case ETokenType.Caret:
                    error = "Arithmetic";
                    break;

                case ETokenType.Less:
                case ETokenType.LessEqual:
                case ETokenType.Greater:
                case ETokenType.GreaterEqual:
                    error = "Comparison";
                    break;

                case ETokenType.Equal:
                case ETokenType.NotEqual:
                    error = "Equality";
                    break;

                case ETokenType.Is:
                case ETokenType.Or:
                case ETokenType.Not:
                case ETokenType.And:
                    error = "Boolean";
                    break;

                default:
                    error = "Operator";
                    break;
            }

            string op = Source.GetString(node.Token.span);

            Diagnostics.Report(ESeverity.Error, $"{error} '{op}' not defined for ({l}, {r}).", node.Token);
            return EDataType.None;
        }

        public EDataType Visit(ExprGroup node)
        {
            return node.Inner.Accept(this);
        }

        public EDataType Visit(ExprUnaryPostfix node)
        {
            throw new NotImplementedException();
        }

        public EDataType Visit(ExprUnaryPrefix node)
        {
            throw new NotImplementedException();
        }

        public EDataType Visit(ExprWithUnit node)
        {
            return node.Inner.Accept(this);
        }
    }
}
