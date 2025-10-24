using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using TriScript.UnitHandling;

namespace TriScript.Parsing
{
    public sealed class VisitorUnit : INodeVisitor<Dim>
    {
        public VisitorUnit(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack Scope { get; set; }
        public Source Source { get; set; }
        public Diagnostics Diagnostics { get; set; }

        public bool Visit(ExprIdentifier node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprAssignment node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprBinary node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprGroup node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprLiteral node, out Dim result)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public bool Visit(StmtBlock node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtPrint node, out Dim result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtProgram node, out Dim result)
        {
            throw new NotImplementedException();
        }

        //public Dim? Visit(ExprIdentifier node)
        //{
        //    string id = Source.GetString(node.Token.span);
        //    if (!ScopeStack.Current.TryGet(id, out Variable var))
        //    {
        //        return var.Eval.dim;
        //    }
        //    return Dim.None;
        //}

        //public Dim? Visit(ExprAssignment node)
        //{
        //    string id = Source.GetString(node.Token.span);

        //    if (!ScopeStack.Current.TryGet(id, out Variable var))
        //    {
        //        return node.Value.Accept(this);
        //    }

        //    Dim cur = var.Eval.dim;
        //    Dim? set = node.Value.Accept(this);

        //    if (!set.HasValue) return null;

        //    Dim newDim = set.Value;
        //    if (!cur.Equals(newDim))
        //    {
        //        string msg = $"Cannot assign value with dimension {newDim} to variable '{id}' with dimension {cur}.";
        //        Diagnostics.Report(ESeverity.Error, msg, node.Token);
        //        return null;
        //    }
        //    return set;
        //}

        //public Dim? Visit(ExprBinary node)
        //{
        //    Dim? ll = node.Left.Accept(this);
        //    Dim? rr = node.Right.Accept(this);
        //    if (!ll.HasValue || !rr.HasValue) return null;

        //    Dim l = ll.Value;
        //    Dim r = rr.Value;

        //    string op = Source.GetString(node.Token.span);

        //    switch (node.Token.type)
        //    {
        //        case ETokenType.Plus:
        //        case ETokenType.Minus:
        //            if (!l.Equals(r))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Operator '{op}' requires matching dimensions; got {l} and {r}.",
        //                    node.Token);
        //                return null;
        //            }
        //            return l;

        //        case ETokenType.Star:
        //            return Dim.Sum(l, r);

        //        case ETokenType.Slash:
        //            return Dim.Div(l, r);

        //        case ETokenType.Caret:
        //            if (!r.Equals(Dim.None))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Exponent '{op}' requires dimensionless right operand; got {r}.",
        //                    node.Right.Token);
        //                return null;
        //            }

        //            if (!TryGetIntegerExponent(node.Right, Evaluator, out int p))
        //            {
        //                // allow dimensionless base with non-integer exponent (result dimensionless),
        //                // otherwise it's invalid to raise a dimensional quantity to a non-integer power
        //                if (!l.Equals(Dim.None))
        //                {
        //                    Diagnostics.Report(
        //                        ESeverity.Error,
        //                        $"Non-integer exponent not allowed for dimensional base {l}.",
        //                        node.Token);
        //                    return null;
        //                }
        //                return Dim.None; // dimless^real → dimless
        //            }

        //            return Dim.Pow(l, p);

        //        case ETokenType.Less:
        //        case ETokenType.LessEqual:
        //        case ETokenType.Greater:
        //        case ETokenType.GreaterEqual:
        //            if (!l.Equals(r))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Comparison '{op}' requires matching dimensions; got {l} and {r}.",
        //                    node.Token);
        //                return null;
        //            }
        //            return Dim.None;

        //        case ETokenType.Equal:
        //        case ETokenType.NotEqual:
        //            if (!l.Equals(r))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Equality '{op}' requires matching dimensions; got {l} and {r}.",
        //                    node.Token);
        //                return null;
        //            }
        //            return Dim.None;

        //        case ETokenType.Is:
        //        case ETokenType.Or:
        //        case ETokenType.Not:
        //        case ETokenType.And:
        //            if (!l.Equals(Dim.None))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Operator '{op}' requires dimensionless left operand; got {l}.",
        //                    node.Left.Token);
        //                return null;
        //            }
        //            if (!r.Equals(Dim.None))
        //            {
        //                Diagnostics.Report(
        //                    ESeverity.Error,
        //                    $"Operator '{op}' requires dimensionless right operand; got {r}.",
        //                    node.Right.Token);
        //                return null;
        //            }
        //            return Dim.None;

        //        default:
        //            Diagnostics.Report(
        //           ESeverity.Error,
        //           $"Operator '{op}' not supported in unit analysis.",
        //           node.Token);
        //            return null;
        //    }
        //}

        //static bool TryGetIntegerExponent(Expr expr, VisitorEval eval, out int p)
        //{
        //    p = 0;

        //    Value v;
        //    try
        //    {
        //        v = expr.Accept(eval);
        //    }
        //    catch
        //    {
        //        // Not evaluable at compile-time (depends on runtime data, etc.)
        //        return false;
        //    }

        //    if (!v.type.IsNumeric())
        //        return false;

        //    if (double.IsNaN(v.real) || double.IsInfinity(v.real))
        //        return false;

        //    // If it's an integer-typed value, just range-check and return
        //    if (v.type.IsInteger())
        //    {
        //        // v.real should be integral already; guard against overflow.
        //        if (v.real < int.MinValue || v.real > int.MaxValue)
        //            return false;

        //        // extra paranoia in case integer carried as double
        //        double rounded = Math.Round(v.real);
        //        if (Math.Abs(v.real - rounded) > 0) // exactly integral expected for integer types
        //            return false;

        //        p = (int)rounded;
        //        return true;
        //    }

        //    // If it's a real, accept it iff it's mathematically an integer within int range
        //    const double EPS = 1e-12;
        //    double r = v.real;
        //    double k = Math.Round(r);

        //    if (Math.Abs(r - k) <= EPS && k >= int.MinValue && k <= int.MaxValue)
        //    {
        //        p = (int)k;
        //        return true;
        //    }

        //    return false;
        //}

        //public Dim? Visit(ExprGroup node)
        //{
        //    return node.Inner.Accept(this);
        //}

        //public Dim? Visit(ExprNumeric node)
        //{
        //    return Dim.None;
        //}

        //public Dim? Visit(ExprUnaryPostfix node)
        //{
        //    throw new NotImplementedException();
        //}

        //public Dim? Visit(ExprUnaryPrefix node)
        //{
        //    throw new NotImplementedException();
        //}

        //public Dim? Visit(ExprWithUnit node)
        //{
        //    var eval = node.Inner.Accept(this);
        //    if (!eval.HasValue) return null;

        //    Dim val = eval.Value;
        //    if (val.Equals(Dim.None) || val.Equals(node.Eval.dim))
        //    {
        //        return node.Eval.dim;
        //    }
        //    return null;
        //}
    }
}
