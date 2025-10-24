using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class VisitorEval : INodeVisitor<Value>
    {
        public VisitorEval(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack Scope { get; }
        public Source Source { get; }
        public Diagnostics Diagnostics { get; }

        public bool Visit(ExprAssignment node, out Value result)
        {
            string name = node.Assignee.ToString();

            bool declared = !Scope.Current.TryGet(name, out Variable var);
            if (declared)
            {
                var = new Variable(name);
                Scope.Current.Declare(var);
            }

            if (!node.Value.Accept(this, out result))
            {
                result = Value.Nothing;
                return false;
            }

            var.Value = result;
            return true;
        }

        public bool Visit(ExprBinary node, out Value result)
        {
            if (!node.Left.Accept(this, out Value l) || !node.Right.Accept(this, out Value r))
            {
                result = Value.Nothing;
                return false;
            }

            switch (node.Token.type)
            {
                case ETokenType.Plus:
                    result = l + r;
                    break;
                case ETokenType.Minus:
                    result = l - r;
                    break;
                case ETokenType.Star:
                    result = l * r;
                    break;
                case ETokenType.Slash:
                    result = l / r;
                    break;
                case ETokenType.Caret:
                    result = Value.Pow(in l, in r);
                    break;

                case ETokenType.Less:
                    result = l < r;
                    break;
                case ETokenType.LessEqual:
                    result = l <= r;
                    break;
                case ETokenType.Greater:
                    result = l > r;
                    break;
                case ETokenType.GreaterEqual:
                    result = l >= r;
                    break;
                case ETokenType.Equal:
                    result = l == r;
                    break;

                case ETokenType.NotEqual:
                    result = l != r;
                    break;

                case ETokenType.Or:
                    result = l | r;
                    break;
                case ETokenType.And:
                    result = l & r;
                    break;
                    
                default:
                    throw new NotImplementedException($"'{node.Token.type}'");
            }
            return true;
        }

        public bool Visit(ExprGroup node, out Value result)
        {
            return node.Inner.Accept(this, out result);
        }

        public bool Visit(ExprLiteralInteger node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public bool Visit(ExprLiteralReal node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public bool Visit(ExprLiteralString node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public bool Visit(ExprLiteralSymbol node, out Value result)
        {
            string name = node.ToString();
            if (Scope.Current.TryGet(node.ToString(), out Variable var))
            {
                result = var.Value;
                return true;
            }

            Diagnostics.Report(Source, ESeverity.Error, $"Use of undeclared variable '{name}'.", node.Token);

            result = Value.Nothing;
            return false;
        }

        public bool Visit(ExprUnaryPostfix node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPrefix node, out Value result)
        {
            if (!node.Right.Accept(this, out Value operand))
            {
                result = Value.Nothing;
                return false;
            }

            switch (node.Token.type)
            {
                case ETokenType.Plus:
                    result = +operand;
                    break;

                case ETokenType.Minus:
                    result = -operand;
                    break;

                case ETokenType.Not:
                    result = !operand;
                    break;

                default:
                    throw new NotImplementedException($"'{node.Token.type}'");
            }
            return true;
        }

        public bool Visit(ExprWithUnit node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtBlock node, out Value result)
        {
            Scope.Open();

            result = Value.Nothing;
            foreach (var stmt in node.Statements)
            {
                if (!stmt.Accept(this, out _))
                {
                    return false;
                }
            }

            Scope.Close();
            return true;
        }

        public bool Visit(StmtPrint node, out Value result)
        {
            result = Value.Nothing;

            if (node.Arguments.Count > 1)
            {
                Diagnostics.Report(Source, ESeverity.Error, $"'print' expects 0 or 1 arguments but got '{node.Arguments.Count}'.", node.Token);
                return false;
            }

            if (node.Arguments.Count == 0)
            {
                Console.WriteLine();
                return true;
            }

            if (!node.Arguments[0].Accept(this, out Value inner))
            {
                return false;
            }

            Console.WriteLine(inner);
            return true;
        }

        public bool Visit(StmtProgram node, out Value result)
        {
            return node.Block.Accept(this, out result);
        }

        public bool Visit(StmtExpr node, out Value result)
        {
            return node.Inner.Accept(this, out result);
        }
    }
}
