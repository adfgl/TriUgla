using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using TriScript.UnitHandling;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TriScript.Parsing
{
    public class ExprEvaluateVisitor : IExprVisitor<Value>
    {

        public ExprEvaluateVisitor(ScopeStack stack, Source source, Diagnostics diagnostics, UnitRegistry registry)
        {
            ScopeStack = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack ScopeStack { get; set; }
        public Source Source { get; set; }
        public Diagnostics Diagnostics { get; set; }
        public UnitRegistry Registry { get; set; }

        public Value Visit(ExprIdentifier node)
        {
            throw new NotImplementedException();
        }

        public Value Visit(ExprAssignment node)
        {
            throw new NotImplementedException();
        }

        public Value Visit(ExprBinary node)
        {
            Value l = node.Left.Accept(this);
            Value r = node.Right.Accept(this);

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
        }

        public Value Visit(ExprGroup node)
        {
            return node.Inner.Accept(this);
        }

        public Value Visit(ExprNumeric node)
        {
            return node.Value;
        }

        public Value Visit(ExprUnaryPostfix node)
        {
            throw new NotImplementedException();
        }

        public Value Visit(ExprUnaryPrefix node)
        {
            throw new NotImplementedException();
        }

        public Value Visit(ExprWithUnit node)
        {
            throw new NotImplementedException();
        }
    }
}
