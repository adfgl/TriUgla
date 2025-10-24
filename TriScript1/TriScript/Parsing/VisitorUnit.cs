using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using TriScript.UnitHandling;

namespace TriScript.Parsing
{
    public sealed class VisitorUnit : INodeVisitor<Dim>
    {
        readonly VisitorEval _visitorEval;

        public VisitorUnit(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
            Diagnostics = diagnostics;
            _visitorEval = new VisitorEval(stack, source, diagnostics);
        }

        public ScopeStack Scope { get; }
        public Source Source { get; }
        public Diagnostics Diagnostics { get; }    

        public bool Visit(ExprAssignment node, out Dim result)
        {
            string id = node.Assignee.ToString();
            if (Scope.Current.TryGet(id, out Variable var) && node.Value.Accept(this, out Dim actual))
            {
                Dim expect = var.Eval.dim;

                Dim dif = expect - actual;
                if (dif == Dim.None)
                {
                    result = actual;
                    return true;
                }
                Diagnostics.Report(Source, ESeverity.Error, $"Cannot assign {id}[{expect}] !={actual}.", node.Value.Token);
            }
            result = Dim.None;
            return false;
        }

        public bool Visit(ExprBinary node, out Dim result)
        {
            result = Dim.None;
            if (!node.Left.Accept(this, out Dim l) ||
                !node.Right.Accept(this, out Dim r))
            {
                return false;
            }

            string op = Source.GetString(node.Token.span);
            switch (node.Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                    if (l != r)
                    {
                        Diagnostics.Report(Source,
                            ESeverity.Error,
                            $"Operator '{op}' requires matching dimensions; got {l} and {r}.",
                            node.Token);
                        return false;
                    }
                    result = l;
                    return true;

                case ETokenType.Star:
                    result = l + r;
                    return true;

                case ETokenType.Slash:
                    result = l - r;
                    return true;

                case ETokenType.Caret:
                    if (r != Dim.None)
                    {
                        Diagnostics.Report(Source, 
                            ESeverity.Error,
                            $"Exponent '{op}' requires dimensionless right operand; got {r}.",
                            node.Right.Token);
                        return false;
                    }

                    if (!TryGetIntegerExponent(node.Right, _visitorEval, out int p))
                    {
                        if (!l.Equals(Dim.None))
                        {
                            Diagnostics.Report(Source,
                                ESeverity.Error,
                                $"Non-integer exponent not allowed for dimensional base {l}.",
                                node.Token);
                            return false;
                        }
                        result = Dim.None;
                        return true; // dimless^real → dimless
                    }

                    result = Dim.Pow(l, p);
                    return true;

                case ETokenType.Less:
                case ETokenType.LessEqual:
                case ETokenType.Greater:
                case ETokenType.GreaterEqual:
                    if (l != r)
                    {
                        Diagnostics.Report(Source, 
                            ESeverity.Error,
                            $"Comparison '{op}' requires matching dimensions; got {l} and {r}.",
                            node.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                case ETokenType.Equal:
                case ETokenType.NotEqual:
                    if (!l.Equals(r))
                    {
                        Diagnostics.Report(Source,
                            ESeverity.Error,
                            $"Equality '{op}' requires matching dimensions; got {l} and {r}.",
                            node.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                case ETokenType.Or:
                case ETokenType.Not:
                case ETokenType.And:
                    if (!l.Equals(Dim.None))
                    {
                        Diagnostics.Report(Source,
                            ESeverity.Error,
                            $"Operator '{op}' requires dimensionless left operand; got {l}.",
                            node.Left.Token);
                        return false;
                    }
                    if (!r.Equals(Dim.None))
                    {
                        Diagnostics.Report(Source,
                            ESeverity.Error,
                            $"Operator '{op}' requires dimensionless right operand; got {r}.",
                            node.Right.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                default:
                    Diagnostics.Report(Source,
                   ESeverity.Error,
                   $"Operator '{op}' not supported in unit analysis.",
                   node.Token);
                    result = Dim.None;
                    return true;
            }
        }

        public bool Visit(ExprGroup node, out Dim result)
        {
            return node.Inner.Accept(this, out result);
        }

        public bool Visit(ExprLiteralInteger node, out Dim result)
        {
            result = Dim.None;
            return true;
        }

        public bool Visit(ExprLiteralReal node, out Dim result)
        {
            result = Dim.None;
            return true;
        }

        public bool Visit(ExprLiteralString node, out Dim result)
        {
            result = Dim.None;
            return true;
        }

        public bool Visit(ExprLiteralSymbol node, out Dim result)
        {
            if (Scope.Current.TryGet(node.ToString(), out Variable var))
            {
                result = var.Eval.dim;
                return true;
            }
            result = Dim.None;
            return false;
        }

        public bool Visit(ExprUnaryPostfix node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPrefix node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprWithUnit node, out Dim result)
        {
            if (node.Inner.Accept(this, out Dim expect))
            {
                Dim actual = node.Eval.dim;

                Dim dif = expect - actual;
                if (dif == Dim.None)
                {
                    result = actual;
                    return true;
                }
                Diagnostics.Report(Source, ESeverity.Error, $"Cannot cast [{expect}] to [{actual}].", node.Token);
            }

            result = Dim.None;
            return false;
        }

        public bool Visit(StmtBlock node, out Dim result)
        {
            foreach (var item in node.Statements)
            {
                if (!item.Accept(this, out _))
                {
                    break;
                }
            }

            result = Dim.None;
            return true;
        }

        public bool Visit(StmtPrint node, out Dim result)
        {
            result = Dim.None;
            return true;
        }

        public bool Visit(StmtProgram node, out Dim result)
        {
            result = Dim.None;
            return true;
        }

        static bool TryGetIntegerExponent(Expr expr, VisitorEval eval, out int p)
        {
            p = 0;

            if (!expr.Accept(eval, out Value v))
            {
                return false;
            }

            if (!v.type.IsNumeric())
                return false;

            if (double.IsNaN(v.real) || double.IsInfinity(v.real))
                return false;

            if (v.type.IsInteger())
            {
                p = (int)v.real;
                return true;
            }

            const double EPS = 1e-12;
            double r = v.real;
            double k = Math.Round(r);

            if (Math.Abs(r - k) <= EPS && k >= int.MinValue && k <= int.MaxValue)
            {
                p = (int)k;
                return true;
            }

            return false;
        }

        public bool Visit(StmtExpr node, out Dim result)
        {
            return node.Accept(this, out result);
        }

        
    }
}
