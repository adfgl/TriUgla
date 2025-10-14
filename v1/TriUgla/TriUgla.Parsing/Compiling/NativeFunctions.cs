using System.Xml.Linq;

namespace TriUgla.Parsing.Compiling
{
    public static class NativeFunctions
    {
        public static TuValue Print(TuValue[] args)
        {
            if (args.Length == 0) Console.WriteLine();
            else Console.WriteLine(args[0].AsString());
            return TuValue.Nothing;
        }

        public static TuValue Min(TuValue[] args)
        {
            if (args.Length == 0) throw new Exception("Min() needs at least one argument.");
            double min = double.PositiveInfinity;

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
                        throw new Exception("Min(): unsupported argument type " + item.type);
                }
            }
            return new TuValue(min);
        }

        public static TuValue Max(TuValue[] args)
        {
            if (args.Length == 0) throw new Exception("Max() needs at least one argument.");
            double max = double.NegativeInfinity;

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
                        throw new Exception("Max(): unsupported argument type " + item.type);
                }
            }
            return new TuValue(max);
        }

        public static TuValue ApplyMathFunc(TuValue[] args, Func<double, double> func, string name)
        {
            if (args.Length != 1)
                throw new Exception($"{name}() expects exactly one argument.");

            TuValue value = args[0];
            switch (value.type)
            {
                case EDataType.Numeric:
                    return new TuValue(func(value.AsNumeric()));
                case EDataType.Tuple:
                    return new TuValue(new TuTuple(value.AsTuple(), func)); // elementwise
                default:
                    throw new Exception($"Unexpected data type '{value.type}' in {name}().");
            }
        }

        private static TuValue ApplyMathFunc2(
            TuValue[] args,
            Func<double, double, double> func,
            string name)
        {
            if (args.Length != 2)
                throw new Exception($"{name}() expects exactly two arguments.");

            TuValue a = args[0];
            TuValue b = args[1];

            // number ∘ number
            if (a.type == EDataType.Numeric && b.type == EDataType.Numeric)
                return new TuValue(func(a.AsNumeric(), b.AsNumeric()));

            // tuple ∘ number  (broadcast scalar)
            if (a.type == EDataType.Tuple && b.type == EDataType.Numeric)
            {
                double sb = b.AsNumeric();
                return new TuValue(new TuTuple(a.AsTuple(), x => func(x, sb)));
            }

            // number ∘ tuple  (broadcast scalar)
            if (a.type == EDataType.Numeric && b.type == EDataType.Tuple)
            {
                double sa = a.AsNumeric();
                return new TuValue(new TuTuple(b.AsTuple(), x => func(sa, x)));
            }

            // tuple ∘ tuple  (elementwise) — requires a binary-map ctor or Zip on TuTuple
            if (a.type == EDataType.Tuple && b.type == EDataType.Tuple)
            {
                throw new NotSupportedException(
                    $"{name}(): tuple ∘ tuple not implemented; add a binary TuTuple-map overload.");
            }

            throw new Exception($"{name}(): unsupported argument types {a.type} and {b.type}.");
        }

        public static TuValue Acos(TuValue[] args) => ApplyMathFunc(args, Math.Acos, nameof(Acos));
        public static TuValue Asin(TuValue[] args) => ApplyMathFunc(args, Math.Asin, nameof(Asin));
        public static TuValue Atan(TuValue[] args) => ApplyMathFunc(args, Math.Atan, nameof(Atan));
        public static TuValue Sin(TuValue[] args) => ApplyMathFunc(args, Math.Sin, nameof(Sin));
        public static TuValue Sinh(TuValue[] args) => ApplyMathFunc(args, Math.Sinh, nameof(Sinh));
        public static TuValue Cosh(TuValue[] args) => ApplyMathFunc(args, Math.Cosh, nameof(Cosh));
        public static TuValue Tanh(TuValue[] args) => ApplyMathFunc(args, Math.Tanh, nameof(Tanh));
        public static TuValue Cos(TuValue[] args) => ApplyMathFunc(args, Math.Cos, nameof(Cos));
        public static TuValue Tan(TuValue[] args) => ApplyMathFunc(args, Math.Tan, nameof(Tan));
        public static TuValue Exp(TuValue[] args) => ApplyMathFunc(args, Math.Exp, nameof(Exp));
        public static TuValue Log(TuValue[] args) => ApplyMathFunc(args, Math.Log, nameof(Log));
        public static TuValue Log10(TuValue[] args) => ApplyMathFunc(args, Math.Log10, nameof(Log10));
        public static TuValue Sqrt(TuValue[] args) => ApplyMathFunc(args, Math.Sqrt, nameof(Sqrt));
        public static TuValue Fabs(TuValue[] args) => ApplyMathFunc(args, Math.Abs, nameof(Fabs));
        public static TuValue Ceil(TuValue[] args) => ApplyMathFunc(args, Math.Ceiling, nameof(Ceil));
        public static TuValue Floor(TuValue[] args) => ApplyMathFunc(args, Math.Floor, nameof(Floor));

        public static TuValue Round(TuValue[] args)
        {
            if (args.Length == 0)
                throw new Exception("Round() expects at least one argument.");

            TuValue value = args[0];

            int digits = 0;
            if (args.Length >= 2)
            {
                TuValue arg1 = args[1];
                if (arg1.type != EDataType.Numeric)
                    throw new Exception("Round(): second argument must be numeric.");

                double d = arg1.AsNumeric();

                if (d < 0 || d != Math.Floor(d))
                    throw new Exception("Round(): second argument must be a non-negative integer.");

                digits = (int)d;
            }

            Func<double, double> func = x => Math.Round(x, digits, MidpointRounding.AwayFromZero);

            switch (value.type)
            {
                case EDataType.Numeric:
                    return new TuValue(func(value.AsNumeric()));

                case EDataType.Tuple:
                    return new TuValue(new TuTuple(value.AsTuple(), func));

                default:
                    throw new Exception($"Unexpected data type '{value.type}' in Round().");
            }
        }


        public static TuValue Atan2(TuValue[] args)
            => ApplyMathFunc2(args, (y, x) => Math.Atan2(y, x), nameof(Atan2));

        public static TuValue Hypot(TuValue[] args)
        {
            if (args.Length != 2)
                throw new Exception("Hypot() expects exactly two arguments.");

            TuValue a = args[0];
            TuValue b = args[1];

            // both numeric
            if (a.type == EDataType.Numeric && b.type == EDataType.Numeric)
            {
                double x = a.AsNumeric();
                double y = b.AsNumeric();

                double ax = Math.Abs(x);
                double ay = Math.Abs(y);
                double m = Math.Max(ax, ay);
                if (m == 0) return new TuValue(0.0);
                ax /= m;
                ay /= m;
                return new TuValue(m * Math.Sqrt(ax * ax + ay * ay));
            }

            // tuple ∘ scalar
            if (a.type == EDataType.Tuple && b.type == EDataType.Numeric)
            {
                double sb = b.AsNumeric();
                return new TuValue(new TuTuple(a.AsTuple(), x =>
                {
                    double ax = Math.Abs(x);
                    double ay = Math.Abs(sb);
                    double m = Math.Max(ax, ay);
                    if (m == 0) return 0.0;
                    ax /= m;
                    ay /= m;
                    return m * Math.Sqrt(ax * ax + ay * ay);
                }));
            }

            // scalar ∘ tuple
            if (a.type == EDataType.Numeric && b.type == EDataType.Tuple)
            {
                double sa = a.AsNumeric();
                return new TuValue(new TuTuple(b.AsTuple(), y =>
                {
                    double ax = Math.Abs(sa);
                    double ay = Math.Abs(y);
                    double m = Math.Max(ax, ay);
                    if (m == 0) return 0.0;
                    ax /= m;
                    ay /= m;
                    return m * Math.Sqrt(ax * ax + ay * ay);
                }));
            }

            // tuple ∘ tuple (elementwise) — implement later if you have a binary map
            if (a.type == EDataType.Tuple && b.type == EDataType.Tuple)
            {
                throw new NotSupportedException("Hypot(tuple, tuple) not supported yet; add a binary-map TuTuple overload.");
            }

            throw new Exception($"Hypot(): unsupported argument types {a.type} and {b.type}.");
        }


        public static TuValue Fmod(TuValue[] args)
            => ApplyMathFunc2(args,
                (a, b) =>
                {
                    if (b == 0) return double.NaN;
                    // C's fmod: a - b * trunc(a/b)  (keeps sign of a)
                    return a - b * Math.Truncate(a / b);
                },
                nameof(Fmod));

        public static TuValue Modulo(TuValue[] args) => Fmod(args);

        public static TuValue Rand(TuValue[] args)
        {
            if (args.Length != 1) throw new Exception("Rand() expects exactly one argument.");
            TuValue a = args[0];

            switch (a.type)
            {
                case EDataType.Numeric:
                    {
                        double bound = a.AsNumeric();
                        if (double.IsNaN(bound) || double.IsInfinity(bound)) return new TuValue(double.NaN);
                        if (bound < 0) // keep semantics simple: negative bound -> uniform in [bound, 0]
                            return new TuValue(bound + Random.Shared.NextDouble() * (-bound));
                        return new TuValue(Random.Shared.NextDouble() * bound);
                    }

                case EDataType.Tuple:
                    return new TuValue(new TuTuple(
                        a.AsTuple(),
                        bound =>
                        {
                            if (double.IsNaN(bound) || double.IsInfinity(bound)) return double.NaN;
                            if (bound < 0) return bound + Random.Shared.NextDouble() * (-bound);
                            return Random.Shared.NextDouble() * bound;
                        }));

                default:
                    throw new Exception("Rand(): unsupported argument type " + a.type);
            }
        }
    }

}
