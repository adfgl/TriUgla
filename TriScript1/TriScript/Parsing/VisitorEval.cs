using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public sealed class VisitorEval : VisitorBase<Value>
    {
        public VisitorEval(ScopeStack stack, Source source, Diagnostics diagnostics)
            : base(stack, source, diagnostics) { }

        static string SymName(ExprLiteralSymbol s) => s.ToString();

        // Resolve existing variable (nearest scope). If missing, declare in Current.
        bool TryGetOrDeclareInCurrent(string name, out Variable v)
        {
            if (TryResolve(name, out v))
                return true;

            v = new Variable(name);
            Scope.Current.Declare(v);
            return true;
        }

        public override bool Visit(StmtProgram node, out Value result)
        {
            using (WithScope())
            {
                return node.Block.Accept(this, out result);
            }
        }

        public override bool Visit(StmtBlock node, out Value result)
        {
            using (WithScope())
            {
                result = Value.Nothing;
                foreach (var stmt in node.Statements)
                {
                    if (!stmt.Accept(this, out _))
                        return false;
                }
                return true;
            }
        }

        public override bool Visit(StmtExpr node, out Value result)
            => node.Inner.Accept(this, out result);

        public override bool Visit(StmtPrint node, out Value result)
        {
            result = Value.Nothing;

            if (node.Arguments.Count > 1)
            {
                Report($"'print' expects 0 or 1 arguments but got '{node.Arguments.Count}'.", node.Token);
                return false;
            }

            if (node.Arguments.Count == 0)
            {
                Console.WriteLine();
                return true;
            }

            if (!node.Arguments[0].Accept(this, out var inner))
                return false;

            Console.WriteLine(inner);
            return true;
        }

        public override bool Visit(ExprAssignment node, out Value result)
        {
            result = Value.Nothing;

            var name = SymName(node.Assignee);

            if (!node.Value.Accept(this, out var rhs))
                return false;

            // Assign to nearest existing scope; if not found, declare in current.
            if (!TryGetOrDeclareInCurrent(name, out var variable))
                return false;

            variable.Value = rhs;
            result = rhs;
            return true;
        }

        public override bool Visit(ExprBinary node, out Value result)
        {
            result = Value.Nothing;

            if (!node.Left.Accept(this, out var l) || !node.Right.Accept(this, out var r))
                return false;

            switch (node.Token.type)
            {
                case ETokenType.Plus: result = l + r; break;
                case ETokenType.Minus: result = l - r; break;
                case ETokenType.Star: result = l * r; break;
                case ETokenType.Slash: result = l / r; break;
                case ETokenType.Caret: result = Value.Pow(in l, in r); break;

                case ETokenType.Less: result = l < r; break;
                case ETokenType.LessEqual: result = l <= r; break;
                case ETokenType.Greater: result = l > r; break;
                case ETokenType.GreaterEqual: result = l >= r; break;
                case ETokenType.Equal: result = l == r; break;
                case ETokenType.NotEqual: result = l != r; break;

                case ETokenType.Or: result = l | r; break;
                case ETokenType.And: result = l & r; break;

                default:
                    throw new NotImplementedException($"Binary op '{node.Token.type}'");
            }
            return true;
        }

        public override bool Visit(ExprGroup node, out Value result)
            => node.Inner.Accept(this, out result);

        public override bool Visit(ExprLiteralInteger node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public override bool Visit(ExprLiteralReal node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public override bool Visit(ExprLiteralString node, out Value result)
        {
            result = node.Value;
            return true;
        }

        public override bool Visit(ExprLiteralSymbol node, out Value result)
        {
            var name = node.ToString();

            if (TryResolve(name, out var v))
            {
                result = v.Value;
                return true;
            }

            Report($"Use of undeclared variable '{name}'.", node.Token);
            result = Value.Nothing;
            return false;
        }

        public override bool Visit(ExprUnaryPrefix node, out Value result)
        {
            result = Value.Nothing;

            if (!node.Right.Accept(this, out var operand))
                return false;

            switch (node.Token.type)
            {
                case ETokenType.Plus: result = +operand; break;
                case ETokenType.Minus: result = -operand; break;
                case ETokenType.Not: result = !operand; break;

                // ++x / --x (if you support them as prefix; otherwise remove these)
                case ETokenType.PlusPlus:
                case ETokenType.MinusMinus:
                    {
                        if (node.Right is not ExprLiteralSymbol sym)
                        {
                            Report("Increment/decrement requires a variable.", node.Token);
                            return false;
                        }

                        var name = SymName(sym);
                        if (!TryResolve(name, out var v))
                        {
                            Report($"Undeclared variable '{name}' in prefix '{Source.GetString(node.Token.span)}'.", node.Token);
                            return false;
                        }

                        // Evaluate current, then mutate, then result = mutated (prefix semantics)
                        var cur = v.Value;
                        var one = Value.One(cur); // supply a helper if needed; otherwise use Value.From(1)
                        var next = (node.Token.type == ETokenType.PlusPlus) ? (cur + one) : (cur - one);
                        v.Value = next;
                        result = next;
                        break;
                    }

                default:
                    throw new NotImplementedException($"Unary op '{node.Token.type}'");
            }
            return true;
        }

        public override bool Visit(ExprUnaryPostfix node, out Value result)
        {
            result = Value.Nothing;

            if (node.Left is not ExprLiteralSymbol sym)
            {
                Report("Postfix operator applies only to variables.", node.Token);
                return false;
            }

            var name = SymName(sym);
            if (!TryResolve(name, out var v))
            {
                Report($"Undeclared variable '{name}' in postfix '{Source.GetString(node.Token.span)}'.", node.Token);
                return false;
            }

            var cur = v.Value;
            var one = Value.One(cur);
            var next = (node.Token.type == ETokenType.PlusPlus) ? (cur + one) : (cur - one);

            // Postfix: result is old value, then mutate
            result = cur;
            v.Value = next;
            return true;
        }

        public override bool Visit(ExprWithUnit node, out Value result)
        {
            if (!node.Inner.Accept(this, out var baseVal))
            {
                result = Value.Nothing;
                return false;
            }

            result = baseVal;
            return true;
        }
    }

}
