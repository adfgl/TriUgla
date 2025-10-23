using TriScript.Scanning;

namespace TriScript.UnitHandling
{
    public abstract class UnitNode
    {
        public TextPosition Start { get; }
        public TextSpan Span { get; }

        protected UnitNode(TextPosition start, TextSpan span)
        {
            Start = start;
            Span = span;
        }

        public override string ToString()
        {
            return UnitPretty.ToCanonicalString(this);
        }

        public abstract Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false);

        public static bool ContainsAffine(UnitNode n, UnitRegistry reg)
        {
            switch (n)
            {
                case UnitSym s:
                    return reg.GetPrefixed(s.Symbol).unit.IsAffine;
                case UnitPow p:
                    return ContainsAffine(p.Base, reg);
                case UnitMul m:
                    return m.Factors.Any(f => ContainsAffine(f, reg));
                case UnitDiv d:
                    return ContainsAffine(d.Numerator, reg) || ContainsAffine(d.Denominator, reg);
                case UnitGroup g:
                    return ContainsAffine(g.Inner, reg);
                default:
                    return false;
            }
        }

        public static int CountAffineSymbols(UnitNode n, UnitRegistry reg)
        {
            int sum = 0;
            switch (n)
            {
                case UnitSym s:
                    return reg.GetPrefixed(s.Symbol).unit.IsAffine ? 1 : 0;
                case UnitPow p:
                    return CountAffineSymbols(p.Base, reg);
                case UnitMul m:
                    foreach (var f in m.Factors) sum += CountAffineSymbols(f, reg);
                    return sum;
                case UnitDiv d:
                    return CountAffineSymbols(d.Numerator, reg) + CountAffineSymbols(d.Denominator, reg);
                case UnitGroup g:
                    return CountAffineSymbols(g.Inner, reg);
                default:
                    return 0;
            }
        }

        public static int CountNonAffineSymbols(UnitNode n, UnitRegistry reg)
        {
            int sum = 0;
            switch (n)
            {
                case UnitSym s:
                    return reg.GetPrefixed(s.Symbol).unit.IsAffine ? 0 : 1;
                case UnitPow p:
                    return CountNonAffineSymbols(p.Base, reg);
                case UnitMul m:
                    foreach (var f in m.Factors) sum += CountNonAffineSymbols(f, reg);
                    return sum;
                case UnitDiv d:
                    return CountNonAffineSymbols(d.Numerator, reg) + CountNonAffineSymbols(d.Denominator, reg);
                case UnitGroup g:
                    return CountNonAffineSymbols(g.Inner, reg);
                default:
                    return 0;
            }
        }
    }

    public sealed class UnitSym : UnitNode
    {
        public string Symbol { get; }

        public UnitSym(string symbol, TextPosition start, TextSpan span)
            : base(start, span)
        {
            Symbol = symbol;
        }

        public override Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false)
        {
            var (u, _) = reg.GetPrefixed(Symbol);
            return u.Dim;
        }
    }

    public sealed class UnitPow : UnitNode
    {
        public UnitNode Base { get; }
        public int Exponent { get; }

        public UnitPow(UnitNode @base, int exponent, TextPosition start, TextSpan span)
            : base(start, span)
        {
            Base = @base;
            Exponent = exponent;
        }

        public override Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false)
        {
            if (checkAffine && ContainsAffine(Base, reg) && Exponent != 1)
                throw new InvalidOperationException($"Cannot raise affine unit to power {Exponent} at {Start}");

            Dim d = Base.EvaluateDim(reg, checkAffine);
            return Dim.Pow(d, Exponent);
        }
    }

    public sealed class UnitMul : UnitNode
    {
        public IReadOnlyList<UnitNode> Factors { get; }

        public UnitMul(IEnumerable<UnitNode> factors, TextPosition start, TextSpan span)
            : base(start, span)
        {
            Factors = factors.ToArray();
        }

        public override Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false)
        {
            if (checkAffine && ContainsAffine(this, reg))
            {
                var cntAffine = CountAffineSymbols(this, reg);
                var cntNonAff = CountNonAffineSymbols(this, reg);

                if (cntAffine >= 1 && cntNonAff >= 1)
                    throw new InvalidOperationException($"Cannot multiply affine units with others at {Start}");
                if (cntAffine > 1)
                    throw new InvalidOperationException($"Cannot multiply two affine units together at {Start}");
            }

            Dim acc = Dim.None;
            foreach (var f in Factors)
                acc = Dim.Sum(acc, f.EvaluateDim(reg, checkAffine));
            return acc;
        }
    }

    public sealed class UnitDiv : UnitNode
    {
        public UnitNode Numerator { get; }
        public UnitNode Denominator { get; }

        public UnitDiv(UnitNode num, UnitNode den, TextPosition start, TextSpan span)
            : base(start, span)
        {
            Numerator = num;
            Denominator = den;
        }

        public override Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false)
        {
            if (checkAffine && (ContainsAffine(Numerator, reg) || ContainsAffine(Denominator, reg)))
                throw new InvalidOperationException($"Cannot divide by/with affine units at {Start}");

            Dim num = Numerator.EvaluateDim(reg, checkAffine);
            Dim den = Denominator.EvaluateDim(reg, checkAffine);
            return Dim.Div(num, den);
        }
    }

    public sealed class UnitGroup : UnitNode
    {
        public UnitNode Inner { get; }

        public UnitGroup(UnitNode inner, TextPosition start, TextSpan span)
            : base(start, span)
        {
            Inner = inner;
        }

        public override Dim EvaluateDim(UnitRegistry reg, bool checkAffine = false)
        {
            return Inner.EvaluateDim(reg, checkAffine);
        }
    }

}
