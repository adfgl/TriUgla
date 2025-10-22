using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public readonly struct OpKey : IEquatable<OpKey>
    {
        public readonly EDataType left, right;
        public readonly ETokenType op;

        public OpKey(ETokenType op, EDataType left, EDataType right)
        {
            this.op = op;
            if (left < right)
            {
                this.left = left;
                this.right = right;
            }
            else
            {
                this.left = right;
                this.right = left;
            }
        }

        public bool Equals(OpKey other) => left == other.left && op == other.op && right == other.right;

        public override bool Equals(object? obj) => obj is OpKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(left, op, right);

        public static bool operator ==(OpKey a, OpKey b) => a.Equals(b);
        public static bool operator !=(OpKey a, OpKey b) => !a.Equals(b);

        public override string ToString() => $"({left}, {right})";
    }

    public class ExprBinary : Expr
    {
        public readonly static IReadOnlyDictionary<OpKey, EDataType> Pairs = new Dictionary<OpKey, EDataType>()
        {
            // arythmetic
            { new OpKey(ETokenType.Plus, EDataType.Real,     EDataType.Real),    EDataType.Real },
            { new OpKey(ETokenType.Plus, EDataType.Real,     EDataType.Integer), EDataType.Real },
            { new OpKey(ETokenType.Plus, EDataType.Integer,  EDataType.Integer), EDataType.Integer },

            { new OpKey(ETokenType.Minus, EDataType.Real,    EDataType.Real),    EDataType.Real },
            { new OpKey(ETokenType.Minus, EDataType.Real,    EDataType.Integer), EDataType.Real },
            { new OpKey(ETokenType.Minus, EDataType.Integer, EDataType.Integer), EDataType.Integer },

            { new OpKey(ETokenType.Star, EDataType.Real,     EDataType.Real),    EDataType.Real },
            { new OpKey(ETokenType.Star, EDataType.Real,     EDataType.Integer), EDataType.Real },
            { new OpKey(ETokenType.Star, EDataType.Integer,  EDataType.Integer), EDataType.Integer },

            { new OpKey(ETokenType.Slash, EDataType.Real,    EDataType.Real),    EDataType.Real },
            { new OpKey(ETokenType.Slash, EDataType.Real,    EDataType.Integer), EDataType.Real },
            { new OpKey(ETokenType.Slash, EDataType.Integer, EDataType.Integer), EDataType.Integer },

            // equality
            { new OpKey(ETokenType.Equal, EDataType.Real,     EDataType.Real),    EDataType.Integer },
            { new OpKey(ETokenType.Equal, EDataType.Real,     EDataType.Integer), EDataType.Integer },
            { new OpKey(ETokenType.Equal, EDataType.Integer,  EDataType.Integer), EDataType.Integer },
            { new OpKey(ETokenType.Equal, EDataType.String,  EDataType.String), EDataType.Integer },

            { new OpKey(ETokenType.NotEqual, EDataType.Real,     EDataType.Real),    EDataType.Integer },
            { new OpKey(ETokenType.NotEqual, EDataType.Real,     EDataType.Integer), EDataType.Integer },
            { new OpKey(ETokenType.NotEqual, EDataType.Integer,  EDataType.Integer), EDataType.Integer },
            { new OpKey(ETokenType.NotEqual, EDataType.String,  EDataType.String), EDataType.Integer },
        };

        public ExprBinary(Expr left, Token op, Expr right) : base(op)
        {
            Left = left;
            Right = right;
        }

        public Expr Left { get; }
        public Expr Right { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            Value l = Left.Evaluate(source, stack, heap);
            Value r = Right.Evaluate(source, stack, heap);
            ETokenType op = Token.type;

            if (l.type.IsNumeric() && r.type.IsNumeric())
            {
                double lr = l.AsDouble();
                double rr = r.AsDouble();
                double result = op switch
                {
                    ETokenType.Plus  => lr + rr,
                    ETokenType.Minus => lr - rr,
                    ETokenType.Star  => lr * rr,
                    ETokenType.Slash => lr / rr,

                    ETokenType.Equal => lr == rr ? 1 : 0,
                    ETokenType.NotEqual => lr != rr ? 1 : 0,
                    ETokenType.Less => lr < rr ? 1 : 0,
                    ETokenType.LessEqaul => lr <= rr ? 1 : 0,
                    ETokenType.Greater => lr > rr ? 1 : 0,
                    ETokenType.GreaterEqual => lr >= rr ? 1 : 0,
                    _ => throw new NotImplementedException(),
                };

                if (l.type == EDataType.Real || r.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new NotImplementedException();
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            EDataType lt = Left.PreviewType(source, stack, diagnostics);
            EDataType rt = Right.PreviewType(source, stack, diagnostics);

            if (lt == EDataType.None || rt == EDataType.None)
            {
                return EDataType.None;
            }

            if (Pairs.TryGetValue(new OpKey(Token.type, lt, rt), out var res))
            {
                return res;
            }

            string op = source.GetString(Token.span);
            if (Token.type is ETokenType.Plus or ETokenType.Minus or ETokenType.Star or ETokenType.Slash)
            {
                diagnostics.Report(ESeverity.Error,
                    $"Arithmetic '{op}' not defined for ({lt}, {rt}).",
                    Token.span);
            }
            else
            {
                diagnostics.Report(ESeverity.Error,
                    $"Operator '{op}' not defined for ({lt}, {rt}).",
                    Token.span);
            }
            return EDataType.None;
        }

        public override bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            if (!Left.EvaluateToSI(src, stack, heap, diagnostics, out double lsi, out Dimension ldim))
            {
                si = double.NaN; dim = Dimension.None;
                return false;
            }

            if (!Right.EvaluateToSI(src, stack, heap, diagnostics, out double rsi, out Dimension rdim))
            {
                si = double.NaN;
                dim = Dimension.None;
                return false;
            }

            ETokenType op = Token.type;
            switch (Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                    if (!ldim.Equals(rdim))
                    {
                        diagnostics.Report(
                            ESeverity.Warning,
                            $"Dimension mismatch in {(op == ETokenType.Plus ? "addition" : "subtraction")}: {ldim} vs {rdim}. Treating result as dimensionless.",
                            Token.span);
                        // still compute numerically in SI
                        si = (op == ETokenType.Plus) ? (lsi + rsi) : (lsi - rsi);
                        dim = Dimension.None;
                        return true;
                    }
                    si = (op == ETokenType.Plus) ? (lsi + rsi) : (lsi - rsi);
                    dim = ldim;
                    return true;

                case ETokenType.Star:
                    si = lsi * rsi;
                    dim = ldim + rdim;
                    return true;

                case ETokenType.Slash:
                    si = lsi / rsi;
                    dim = ldim - rdim;
                    return true;

                default:
                    si = double.NaN;
                    dim = Dimension.None;
                    return false;
            }
        }

        public override UnitEval? EvaluateToUnit(Source s, ScopeStack st, ObjHeap h)
        {
            UnitEval? lu = Left.EvaluateToUnit(s, st, h);
            UnitEval? ru = Right.EvaluateToUnit(s, st, h);

            switch (Token.type)
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                    if (lu.HasValue && ru.HasValue && lu.Value.Dim.Equals(ru.Value.Dim))
                        return lu;          // prefer left on add/sub of same-dim
                    return lu ?? ru;        // one side dimensionless or unknown

                case ETokenType.Star:
                    if (lu.HasValue && ru.HasValue) return lu.Value.Mul(ru.Value);
                    return lu ?? ru;

                case ETokenType.Slash:
                    if (lu.HasValue && ru.HasValue) return lu.Value.Div(ru.Value);
                    if (lu.HasValue) return lu;
                    if (ru.HasValue)
                    {
                        var one = new UnitEval(1.0, Dimension.None, new Dictionary<string, int>());
                        return one.Div(ru.Value);
                    }
                    return null;
            }
            return null;
        }
    }
}
