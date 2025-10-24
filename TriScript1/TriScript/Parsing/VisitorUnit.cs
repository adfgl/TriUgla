using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using TriScript.UnitHandling;

namespace TriScript.Parsing
{
    public sealed class VisitorUnit : VisitorBase<Dim>
    {
        private readonly VisitorEval _eval; // for numeric exponent evaluation

        public VisitorUnit(ScopeStack stack, Source source, Diagnostics diagnostics)
            : base(stack, source, diagnostics)
        {
            _eval = new VisitorEval(stack, source, diagnostics);
        }

        public override bool Visit(StmtProgram node, out Dim result)
        {
            using (WithScope())
            {
                result = Dim.None;
                return node.Block.Accept(this, out _);
            }
        }

        public override bool Visit(StmtBlock node, out Dim result)
        {
            using (WithScope())
            {
                result = Dim.None;
                foreach (var stmt in node.Statements)
                    if (!stmt.Accept(this, out _))
                        return false;
                return true;
            }
        }

        public override bool Visit(StmtPrint node, out Dim result)
        {
            result = Dim.None; // print is side-effect only
            return true;
        }

        public override bool Visit(StmtExpr node, out Dim result)
            => node.Inner.Accept(this, out result); // (fixed) no infinite recursion

        public override bool Visit(ExprAssignment node, out Dim result)
        {
            result = Dim.None;

            var id = SymbolName(node.Assignee);

            if (!node.Value.Accept(this, out var actual))
                return false;

            if (!TryResolve(id, out var variable))
                variable = DeclareHere(id);

            // DimEval is a readonly struct with fields: scale, dim
            var eval = variable.Eval;
            var expect = eval.dim;

            if (expect == Dim.None)
            {
                // First assignment defines the variable’s dimension.
                // Must replace the whole DimEval (cannot mutate readonly fields).
                var scale = eval.scale != 0 ? eval.scale : 1.0;
                variable.Eval = new DimEval(actual, scale);

                result = actual;
                return true;
            }

            var diff = expect - actual;
            if (diff == Dim.None)
            {
                result = actual;
                return true;
            }

            Report($"Cannot assign {id}[{expect}] ≠ {actual}.", node.Value.Token);
            return false;
        }

        public override bool Visit(ExprBinary node, out Dim result)
        {
            result = Dim.None;

            if (!node.Left.Accept(this, out var l) || !node.Right.Accept(this, out var r))
                return false;

            string op = Source.GetString(node.Token.span);

            switch (node.Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                    if (l != r)
                    {
                        Report($"Operator '{op}' requires matching dimensions; got {l} and {r}.", node.Token);
                        return false;
                    }
                    result = l;
                    return true;

                case ETokenType.Star:
                    result = l + r; // multiply -> add dimensions
                    return true;

                case ETokenType.Slash:
                    result = l - r; // divide -> subtract dimensions
                    return true;

                case ETokenType.Caret:
                    // base^exp: exp must be dimensionless
                    if (r != Dim.None)
                    {
                        Report($"Exponent '{op}' requires dimensionless right operand; got {r}.", node.Right.Token);
                        return false;
                    }

                    if (!TryGetIntegerExponent(node.Right, _eval, out int p))
                    {
                        // allow dimless ^ real
                        if (l != Dim.None)
                        {
                            Report($"Non-integer exponent not allowed for dimensional base {l}.", node.Token);
                            return false;
                        }
                        result = Dim.None;
                        return true;
                    }

                    result = Dim.Pow(l, p);
                    return true;

                case ETokenType.Less:
                case ETokenType.LessEqual:
                case ETokenType.Greater:
                case ETokenType.GreaterEqual:
                    if (l != r)
                    {
                        Report($"Comparison '{op}' requires matching dimensions; got {l} and {r}.", node.Token);
                        return false;
                    }
                    result = Dim.None; // comparisons are dimensionless (boolean)
                    return true;

                case ETokenType.Equal:
                case ETokenType.NotEqual:
                    if (l != r)
                    {
                        Report($"Equality '{op}' requires matching dimensions; got {l} and {r}.", node.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                case ETokenType.Or:
                case ETokenType.And:
                case ETokenType.Not:
                    if (l != Dim.None)
                    {
                        Report($"Operator '{op}' requires dimensionless left operand; got {l}.", node.Left.Token);
                        return false;
                    }
                    if ((node.Token.type == ETokenType.And || node.Token.type == ETokenType.Or) && r != Dim.None)
                    {
                        Report($"Operator '{op}' requires dimensionless right operand; got {r}.", node.Right.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                default:
                    Report($"Operator '{op}' not supported in unit analysis.", node.Token);
                    return false;
            }
        }

        public override bool Visit(ExprGroup node, out Dim result)
            => node.Inner.Accept(this, out result);

        public override bool Visit(ExprLiteralInteger node, out Dim result)
        { result = Dim.None; return true; }

        public override bool Visit(ExprLiteralReal node, out Dim result)
        { result = Dim.None; return true; }

        public override bool Visit(ExprLiteralString node, out Dim result)
        { result = Dim.None; return true; }

        public override bool Visit(ExprLiteralSymbol node, out Dim result)
        {
            if (TryResolve(node.ToString(), out var v))
            {
                result = v.Eval.dim;
                return true;
            }
            Report($"Undeclared variable '{node}'.", node.Token);
            result = Dim.None;
            return false;
        }

        public override bool Visit(ExprUnaryPrefix node, out Dim result)
        {
            // +x / -x: dimension preserved; !x: requires dimensionless
            if (!node.Right.Accept(this, out result))
                return false;

            switch (node.Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                    // numeric sign doesn't change dimension
                    return true;

                case ETokenType.Not:
                    if (result != Dim.None)
                    {
                        Report($"Logical 'not' requires dimensionless operand; got {result}.", node.Token);
                        return false;
                    }
                    return true;

                case ETokenType.PlusPlus:
                case ETokenType.MinusMinus:
                    // ++x / --x add/subtract dimensionless 1 -> only valid if x is dimensionless in this model
                    if (node.Right is not ExprLiteralSymbol sym)
                    {
                        Report($"Operator '{Source.GetString(node.Token.span)}' requires a variable.", node.Token);
                        return false;
                    }
                    if (!TryResolve(sym.ToString(), out var v))
                    {
                        Report($"Undeclared variable '{sym}'.", node.Token);
                        return false;
                    }
                    if (v.Eval.dim != Dim.None)
                    {
                        Report($"Operator '{Source.GetString(node.Token.span)}' requires dimensionless variable; got {v.Eval.dim}.", node.Token);
                        return false;
                    }
                    result = Dim.None;
                    return true;

                default:
                    Report($"Unsupported unary operator '{Source.GetString(node.Token.span)}' in unit analysis.", node.Token);
                    return false;
            }
        }

        public override bool Visit(ExprUnaryPostfix node, out Dim result)
        {
            // x++ / x-- must be variable and dimensionless in this model
            result = Dim.None;

            if (node.Left is not ExprLiteralSymbol sym)
            {
                Report("Postfix operator applies only to variables.", node.Operator);
                return false;
            }
            if (!TryResolve(sym.ToString(), out var v))
            {
                Report($"Undeclared variable '{sym}'.", node.Operator);
                return false;
            }
            if (v.Eval.dim != Dim.None)
            {
                Report($"Operator '{Source.GetString(node.Operator.span)}' requires dimensionless variable; got {v.Eval.dim}.", node.Operator);
                return false;
            }
            // result is still Dim.None (type of boolean/number not relevant for dim)
            return true;
        }

        public override bool Visit(ExprWithUnit node, out Dim result)
        {
            // Enforce that expression has the declared unit.
            if (!node.Inner.Accept(this, out var expect))
            {
                result = Dim.None;
                return false;
            }

            var actual = node.Eval.dim; // assuming parser attached resolved unit dimension here

            var dif = expect - actual;
            if (dif == Dim.None)
            {
                result = actual;
                return true;
            }

            Report($"Cannot cast [{expect}] to [{actual}].", node.Token);
            result = Dim.None;
            return false;
        }

        private static bool TryGetIntegerExponent(Expr expr, VisitorEval eval, out int p)
        {
            p = 0;

            if (!expr.Accept(eval, out var v))
                return false;

            if (!v.type.IsNumeric()) return false;
            if (double.IsNaN(v.real) || double.IsInfinity(v.real)) return false;

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
    }

}
