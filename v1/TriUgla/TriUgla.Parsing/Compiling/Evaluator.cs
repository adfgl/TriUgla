using System.Runtime.CompilerServices;
using System.Threading;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Evaluator : INodeVisitor
    {
        Stack _stack = new Stack();

        public Stack Stack => _stack;

        // -------------------- literals / identifiers --------------------

        public TuValue Visit(NodeNumericLiteral n)
        {
            if (double.TryParse(n.Token.value, out double d))
                return new TuValue(d);
            throw new Exception("Invalid numeric literal");
        }

        public TuValue Visit(NodeStringLiteral n)
        {
            return new TuValue(n.Token.value);
        }

        public static bool ValidIdentifier(string id) => true;

        public TuValue Visit(NodeIdentifierLiteral n)
        {
            if (!ValidIdentifier(n.Token.value))
                throw new Exception($"Invalid id '{n.Token.value}'");

            Variable? v = _stack.Current.Get(n.Token);
            if (v is null)
                throw new Exception($"Undefined variable '{n.Token.value}'");
            return v.Value;
        }

        // -------------------- prefix / postfix ++ -- --------------------

        public TuValue Visit(NodePrefixUnary n)
        {
            ETokenType op = n.Token.type;

            // ++x / --x must mutate an lvalue (for now: identifier only)
            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                if (n.Expression is not NodeIdentifierLiteral id)
                    throw new Exception("Prefix ++/-- requires an identifier");

                Variable v = _stack.Current.GetOrDeclare(id.Token);
                if (v.Value.type != EDataType.Numeric)
                    throw new Exception("Prefix ++/-- requires numeric variable");

                double cur = v.Value.AsNumeric();
                double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;
                v.Value = new TuValue(next);
                return v.Value; // prefix returns NEW value
            }

            // numeric +x / -x
            TuValue value = n.Expression.Accept(this);

            if (op == ETokenType.Plus) return value;
            if (op == ETokenType.Minus) return new TuValue(-value.AsNumeric());

            if (op == ETokenType.Not) return new TuValue(!AsBool(value));

            throw new Exception("Unsupported unary op");
        }

        public TuValue Visit(NodePostfixUnary n)
        {
            ETokenType op = n.Token.type;
            if (op != ETokenType.PlusPlus && op != ETokenType.MinusMinus)
                throw new Exception("Unsupported postfix op");

            if (n.Expression is not NodeIdentifierLiteral id)
                throw new Exception("Postfix ++/-- requires an identifier");

            Variable v = _stack.Current.GetOrDeclare(id.Token);
            if (v.Value.type != EDataType.Numeric)
                throw new Exception("Postfix ++/-- requires numeric variable");

            double cur = v.Value.AsNumeric();
            double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;
            v.Value = new TuValue(next);
            return new TuValue(cur); // postfix returns OLD value
        }

        // -------------------- binary ops (+,*,==,&&,||,...) --------------------

        public TuValue Visit(NodeBinary n)
        {
            var op = n.Token.type;

            // Short-circuit first to avoid evaluating RHS unnecessarily
            if (op == ETokenType.Or)
            {
                TuValue left = n.Left.Accept(this);
                if (AsBool(left)) return new TuValue(true);
                TuValue right = n.Right.Accept(this);
                return new TuValue(AsBool(right));
            }
            if (op == ETokenType.And)
            {
                TuValue left = n.Left.Accept(this);
                if (!AsBool(left)) return new TuValue(false);
                TuValue right = n.Right.Accept(this);
                return new TuValue(AsBool(right));
            }

            // Otherwise evaluate both
            TuValue lval = n.Left.Accept(this);
            TuValue rval = n.Right.Accept(this);

            // If both numeric → numeric ops & comparisons
            if (lval.type == EDataType.Numeric && rval.type == EDataType.Numeric)
            {
                double l = lval.AsNumeric();
                double r = rval.AsNumeric();

                switch (op)
                {
                    case ETokenType.Minus: return new TuValue(l - r);
                    case ETokenType.Plus: return new TuValue(l + r);
                    case ETokenType.Slash: return new TuValue(l / r);
                    case ETokenType.Star: return new TuValue(l * r);
                    case ETokenType.Modulo: return new TuValue(l % r);
                    case ETokenType.Power: return new TuValue(Math.Pow(l, r));
                    case ETokenType.Less: return new TuValue(l < r);
                    case ETokenType.Greater: return new TuValue(l > r);
                    case ETokenType.LessOrEqual: return new TuValue(l <= r);
                    case ETokenType.GreaterOrEqual: return new TuValue(l >= r);
                    case ETokenType.EqualEqual: return new TuValue(l == r);
                    case ETokenType.NotEqual: return new TuValue(l != r);
                }
            }

            // String concatenation
            if (op == ETokenType.Plus && (lval.type == EDataType.String || rval.type == EDataType.String))
            {
                return new TuValue(lval.AsString() + rval.AsString());
            }

            // Equality across non-numeric: compare string reps (simple & predictable)
            if (op == ETokenType.EqualEqual)
                return new TuValue(lval.AsString() == rval.AsString());
            if (op == ETokenType.NotEqual)
                return new TuValue(lval.AsString() != rval.AsString());

            throw new Exception("Unsupported binary operation");
        }

        // -------------------- grouping / block --------------------

        public TuValue Visit(NodeGroup n) => n.Expression.Accept(this);

        public TuValue Visit(NodeBlock n)
        {
            TuValue value = TuValue.Nothing;
            foreach (INode exp in n.Nodes)
                value = exp.Accept(this);
            return value;
        }

        // -------------------- if / else if / else --------------------

        public TuValue Visit(NodeIfElse n)
        {
            TuValue ifValue = n.If.Accept(this);
            if (AsBool(ifValue))
                return n.IfBlock.Accept(this);

            foreach (var (elifCond, elifBlock) in n.ElseIfs)
            {
                TuValue cond = elifCond.Accept(this);      // FIX: evaluate condition, not block
                if (AsBool(cond))
                    return elifBlock.Accept(this);
            }

            if (n.ElseBlock is not null)
                return n.ElseBlock.Accept(this);

            return TuValue.Nothing;
        }

        public TuValue Visit(NodeAssignment n)
        {
            // Current node shape: Identifier + Token(op) + Expression
            Token id = n.Identifier.Token;

            // Read current value (default 0 for numerics to help compound ops)
            Variable v = _stack.Current.GetOrDeclare(id);
            TuValue cur = v.Value;

            if (n.Expression is null)
                return v.Value;

            TuValue rhs = n.Expression.Accept(this);

            switch (n.Token.type)
            {
                case ETokenType.Equal:
                    v.Value = rhs.Copy();
                    break;

                case ETokenType.PlusEqual:
                    if (cur.type == EDataType.Tuple)
                    {
                        if (rhs.type == EDataType.Tuple)
                        {
                            TuTuple tpl = cur.Copy().AsTuple()!;
                            tpl.Values.AddRange(rhs.AsTuple()!);
                            v.Value = new TuValue(tpl);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        v.Value = new TuValue(cur.AsNumeric() + rhs.AsNumeric());
                    }
                    break;
                case ETokenType.MinusEqual:
                    v.Value = new TuValue(cur.AsNumeric() - rhs.AsNumeric());
                    break;
                case ETokenType.StarEqual:
                    v.Value = new TuValue(cur.AsNumeric() * rhs.AsNumeric());
                    break;
                case ETokenType.SlashEqual:
                    v.Value = new TuValue(cur.AsNumeric() / rhs.AsNumeric());
                    break;

                default:
                    throw new Exception("Unsupported assignment operator");
            }

            return v.Value;
        }

        // -------------------- ranges / tuples / misc --------------------

        public TuValue Visit(NodeRangeLiteral n)
        {
            TuValue from = n.From.Accept(this);
            if (from.type != EDataType.Numeric) throw new Exception("Range 'from' must be numeric");

            TuValue to = n.To.Accept(this);
            if (to.type != EDataType.Numeric) throw new Exception("Range 'to' must be numeric");

            TuValue by = n.By is null ? new TuValue(1) : n.By.Accept(this);
            if (by.type != EDataType.Numeric) throw new Exception("Range 'by' must be numeric");

            double f = from.AsNumeric();
            double t = to.AsNumeric();
            double b = by.AsNumeric();

            if (b == 0) throw new Exception("Range step cannot be zero");
            if ((t - f) / b < 0) throw new Exception("Range direction and step mismatch");

            return new TuValue(new TuRange(f, t, b));
        }

        public TuValue Visit(NodeFor n)
        {
            Variable i = _stack.Current.GetOrDeclare(n.Identifier.Token);

            TuValue list = n.Range.Accept(this);
            if (list.type == EDataType.Range)
            {
                foreach (double item in list.AsRange())
                {
                    i.Value = new TuValue(item);
                    n.Block.Accept(this);
                }
                return i.Value;
            }
            if (list.type == EDataType.Tuple)
            {
                foreach (double item in list.AsTuple()!.Values)
                {
                    i.Value = new TuValue(item);
                    n.Block.Accept(this);
                }
                return i.Value;
            }

            throw new Exception("For-loop expects range or tuple");
        }

        public TuValue Visit(NodeFun n)
        {
            string function = n.Token.value;

            TuValue[] args = new TuValue[n.Args.Count];
            for (int i = 0; i < args.Length; i++)
                args[i] = n.Args[i].Accept(this);

            switch (function)
            {
                case "Print":
                    if (args.Length == 0) Console.WriteLine();
                    else Console.WriteLine(args[0].AsString());
                    return TuValue.Nothing;

                case "Min":
                    if (args.Length == 2)
                        return new TuValue(Math.Min(args[0].AsNumeric(), args[1].AsNumeric()));
                    throw new Exception("Min expects 2 arguments");

                default:
                    throw new Exception($"Unknown function '{function}'.");
            }
        }

        public TuValue Visit(NodeTupleLiteral n)
        {
            List<double> values = new List<double>(n.Args.Count);
            foreach (INode item in n.Args)
            {
                TuValue v = item.Accept(this);
                if (v.type != EDataType.Numeric)
                    throw new Exception("Tuple elements must be numeric");
                values.Add(v.AsNumeric());
            }
            return new TuValue(new TuTuple(values));
        }

        public TuValue Visit(NodeLengthOf n)
        {
            TuValue value = n.Id.Accept(this);
            if (value.type == EDataType.Tuple)
                return new TuValue(value.AsTuple()!.Values.Count);
            throw new Exception("Length operator applies to tuples");
        }

        public TuValue Visit(NodeValueAt n)
        {
            TuValue tuple = n.Tuple.Accept(this);
            if (tuple.type != EDataType.Tuple)
                throw new Exception("Indexing requires a tuple");

            TuValue index = n.Index.Accept(this);
            if (index.type != EDataType.Numeric)
                throw new Exception("Index must be numeric");

            int i = (int)index.AsNumeric();
            if (Math.Round(index.AsNumeric()) != i)
                throw new Exception("Index must be an integer");

            TuTuple? tpl = tuple.AsTuple();
            if (tpl is null) throw new Exception("Tuple is null");

            if (i < 0 || i >= tpl.Values.Count)
                throw new Exception("Index out of range");

            return new TuValue(tpl.Values[i]);
        }

        // -------------------- helpers --------------------

        static bool AsBool(TuValue v)
        {
            // Use your internal rules; defaults:
            // numeric: != 0; string: non-empty; tuple/range: non-empty if you like
            return v.AsBoolean();
        }
    }

}
