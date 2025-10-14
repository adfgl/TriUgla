using System.Runtime.CompilerServices;
using System.Threading;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Evaluator : INodeVisitor
    {
        Stack _stack = new Stack();

        public Stack Stack => _stack;

        // -------------------- literals / identifiers --------------------

        public TuValue Visit(NodeNumeric n)
        {
            if (double.TryParse(n.Token.value, out double d))
                return new TuValue(d);
            throw new Exception("Invalid numeric literal");
        }

        public TuValue Visit(NodeString n)
        {
            return new TuValue(n.Token.value);
        }

        public static bool ValidIdentifier(string id, out string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                reason = "identifier must contain at least one character";
                return false;
            }

            // verbatim?
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

        string GetId(NodeIdentifier n)
        {
            string id;
            if (n.Id is not null)
            {
                int index = (int)n.Id.Accept(this).AsNumeric();
                id = $"{n.Token.type}({index})";
            }
            else
            {
                id = n.Token.value;
                if (!ValidIdentifier(id, out string reason))
                    throw new Exception($"Invalid id '{id}'. Reason: {reason}");
            }
            return id;
        }

        public TuValue Visit(NodeIdentifier n)
        {
            string id = GetId(n);
            Variable? v = _stack.Current.Get(id);
            if (v is null)
                throw new Exception($"Undefined variable '{id}'");
            return v.Value;
        }

        // -------------------- prefix / postfix ++ -- --------------------

        public TuValue Visit(NodePrefixUnary n)
        {
            ETokenType op = n.Token.type;

            // ++x / --x must mutate an lvalue (for now: identifier only)
            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                if (n.Expression is not NodeIdentifier id)
                    throw new Exception("Prefix ++/-- requires an identifier");

                Variable v = _stack.Current.GetOrDeclare(id.Token.value);
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

            if (n.Expression is not NodeIdentifier id)
                throw new Exception("Postfix ++/-- requires an identifier");

            Variable v = _stack.Current.GetOrDeclare(id.Token.value);
            if (v.Value.type != EDataType.Numeric)
                throw new Exception("Postfix ++/-- requires numeric variable");

            double cur = v.Value.AsNumeric();
            double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;
            v.Value = new TuValue(next);
            return new TuValue(cur); // postfix returns OLD value
        }

        public TuValue Visit(NodeBinary n)
        {
            var op = n.Token.type;

            // Short-circuit first
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

            // Evaluate both
            TuValue lval = n.Left.Accept(this);
            TuValue rval = n.Right.Accept(this);

            // ---------------- NUMERIC ⊙ NUMERIC ----------------
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

            // ---------------- STRING CONCAT ----------------
            if (op == ETokenType.Plus && (lval.type == EDataType.String || rval.type == EDataType.String))
                return new TuValue(lval.AsString() + rval.AsString());

            // ---------------- TUPLE MATH ----------------
            bool lIsTuple = lval.type == EDataType.Tuple;
            bool rIsTuple = rval.type == EDataType.Tuple;
            bool lIsNum = lval.type == EDataType.Numeric;
            bool rIsNum = rval.type == EDataType.Numeric;

            if ((lIsTuple && rIsNum) || (lIsNum && rIsTuple))
            {
                // tuple ⊙ scalar (commutative where appropriate)
                TuTuple t = (lIsTuple ? lval : rval).AsTuple()!;
                double s = (lIsNum ? lval : rval).AsNumeric();

                return op switch
                {
                    ETokenType.Plus => MapTupleScalar(t, s, (a, b) => a + b),
                    ETokenType.Minus => lIsTuple
                                         ? MapTupleScalar(t, s, (a, b) => a - b)   // tuple - scalar
                                         : MapTupleScalar(t, s, (a, b) => b - a),  // scalar - tuple
                    ETokenType.Star => MapTupleScalar(t, s, (a, b) => a * b),
                    ETokenType.Slash => lIsTuple
                                         ? MapTupleScalar(t, s, (a, b) => a / b)   // tuple / scalar
                                         : MapTupleScalar(t, s, (a, b) => b / a),  // scalar / tuple
                    ETokenType.Modulo => lIsTuple
                                         ? MapTupleScalar(t, s, (a, b) => a % b)
                                         : MapTupleScalar(t, s, (a, b) => b % a),
                    ETokenType.Power => lIsTuple
                                         ? MapTupleScalar(t, s, (a, b) => Math.Pow(a, b))   // tuple ^ scalar
                                         : MapTupleScalar(t, s, (a, b) => Math.Pow(b, a)),  // scalar ^ tuple
                    _ => throw new Exception("Unsupported tuple-scalar operator")
                };
            }

            if (lIsTuple && rIsTuple)
            {
                TuTuple lt = lval.AsTuple()!;
                TuTuple rt = rval.AsTuple()!;
                if (lt.Values.Count != rt.Values.Count)
                    throw new Exception("Tuple sizes must match for element-wise operation");

                return op switch
                {
                    ETokenType.Plus => MapTupleTuple(lt, rt, (a, b) => a + b),
                    ETokenType.Minus => MapTupleTuple(lt, rt, (a, b) => a - b),
                    ETokenType.Star => MapTupleTuple(lt, rt, (a, b) => a * b),
                    ETokenType.Slash => MapTupleTuple(lt, rt, (a, b) => a / b),
                    ETokenType.Modulo => MapTupleTuple(lt, rt, (a, b) => a % b),
                    ETokenType.Power => MapTupleTuple(lt, rt, (a, b) => Math.Pow(a, b)),

                    // Equality for tuples: same length & all elements equal
                    ETokenType.EqualEqual => new TuValue(TupleEquals(lt, rt)),
                    ETokenType.NotEqual => new TuValue(!TupleEquals(lt, rt)),

                    _ => throw new Exception("Unsupported tuple-tuple operator")
                };
            }

            if (op == ETokenType.EqualEqual) return new TuValue(lval.AsString() == rval.AsString());
            if (op == ETokenType.NotEqual) return new TuValue(lval.AsString() != rval.AsString());

            throw new Exception("Unsupported binary operation");
        }

        private static TuValue MapTupleScalar(TuTuple t, double s, Func<double, double, double> f)
        {
            var src = t.Values;
            var dst = new double[src.Count];
            for (int i = 0; i < dst.Length; i++) dst[i] = f(src[i], s);
            return new TuValue(new TuTuple(dst));
        }

        private static TuValue MapTupleTuple(TuTuple a, TuTuple b, Func<double, double, double> f)
        {
            var av = a.Values;
            var bv = b.Values;
            if (av.Count != bv.Count) throw new Exception("Tuple sizes must match");
            var dst = new double[av.Count];
            for (int i = 0; i < av.Count; i++) dst[i] = f(av[i], bv[i]);
            return new TuValue(new TuTuple(dst));
        }

        private static bool TupleEquals(TuTuple a, TuTuple b)
        {
            var av = a.Values;
            var bv = b.Values;
            if (av.Count != bv.Count) return false;
            for (int i = 0; i < av.Count; i++)
                if (av[i] != bv[i]) return false;
            return true;
        }

        public TuValue Visit(NodeGroup n) => n.Expression.Accept(this);

        public TuValue Visit(NodeBlock n)
        {
            TuValue value = TuValue.Nothing;
            foreach (INode exp in n.Nodes)
            {
                value = exp.Accept(this);
            }
            return value;
        }

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
            string id = GetId((NodeIdentifier)n.Assignee);
            Variable v = _stack.Current.GetOrDeclare(id);
            TuValue cur = v.Value;

            if (n.Expression is null)
                return v.Value;

            TuValue rhs = n.Expression.Accept(this);

            switch (n.Token.type)
            {
                case ETokenType.Equal:
                    NodeValueAt? at = n.Assignee as NodeValueAt;
                    if (at is not null)
                    {
                        int index = (int)at.Index.Accept(this).AsNumeric();
                        TuTuple? tpl = at.Tuple.Accept(this).AsTuple();
                        tpl.Values[index] = rhs.AsNumeric();
                    }
                    else
                    {
                        v.Value = rhs.Copy();
                    }
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

        public TuValue Visit(NodeRange n)
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
            Variable i = _stack.Current.GetOrDeclare(n.Identifier.Token.value);

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

        public TuValue Visit(NodeFunctionCall n)
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
                    double min = double.MaxValue;
                    if (args.Length == 0) throw new Exception();

                    foreach (TuValue item in args)
                    {
                        switch (item.type)
                        {
                            case EDataType.Numeric:
                                min = Math.Min(min, item.AsNumeric());
                                break;

                            case EDataType.Tuple:
                                min = Math.Min(min, item.AsTuple().Min());
                                break;

                            default:
                                throw new Exception();
                        }
                    }
                    return new TuValue(min);

                case "Max":
                    double max = double.MinValue;
                    if (args.Length == 0) throw new Exception();

                    foreach (TuValue item in args)
                    {
                        switch (item.type)
                        {
                            case EDataType.Numeric:
                                max = Math.Max(max, item.AsNumeric());
                                break;

                            case EDataType.Tuple:
                                max = Math.Max(max, item.AsTuple().Max());
                                break;

                            default:
                                throw new Exception();
                        }
                    }
                    return new TuValue(max);

                default:
                    throw new Exception($"Unknown function '{function}'.");
            }
        }

        public TuValue Visit(NodeTuple n)
        {
            List<double> values = new List<double>(n.Args.Count);
            foreach (INode item in n.Args)
            {
                TuValue v = item.Accept(this);
                switch (v.type)
                {
                    case EDataType.Numeric:
                        values.Add(v.AsNumeric());
                        break;
                    case EDataType.Range:
                        values.AddRange(v.AsTuple()!);
                        break;
                    case EDataType.Tuple:
                        values.AddRange(v.AsTuple()!);
                        break;
                    default:
                        throw new Exception("Tuple elements must be numeric");
                }
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

        static bool AsBool(TuValue v)
        {
            return v.AsBoolean();
        }
    }

}
