using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
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
                switch (op)
                {
                    case ETokenType.Equal:
                        return new Value(lr == rr);
                    case ETokenType.NotEqual:
                        return new Value(lr != rr);
                    case ETokenType.Greater:
                        return new Value(lr > rr);
                    case ETokenType.Less:
                        return new Value(lr < rr);
                    case ETokenType.GreaterEqual:
                        return new Value(lr >= rr);
                    case ETokenType.LessEqaul:
                        return new Value(lr <= rr);
                }

                double result = op switch
                {
                    ETokenType.Plus  => lr + rr,
                    ETokenType.Minus => lr - rr,
                    ETokenType.Star  => lr * rr,
                    ETokenType.Slash => lr / rr,
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
    }
}
