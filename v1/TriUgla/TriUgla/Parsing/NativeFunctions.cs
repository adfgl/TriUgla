using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing
{
    public class NativeFunction
    {
        public string Name { get; }
        public string Description { get; }
        public Func<Value[], Value> Function { get; }

        public NativeFunction(string name, string description, Func<Value[], Value> function)
        {
            Name = name;
            Description = description;
            Function = function;
        }

        public override string ToString() => $"{Name} — {Description}";
    }

    public static class NativeFunctions
    {
        static readonly Dictionary<string, NativeFunction> _functions;
        public static IReadOnlyDictionary<string, NativeFunction> Functions => _functions;

        static NativeFunctions()
        {
            _functions = new Dictionary<string, NativeFunction>(StringComparer.OrdinalIgnoreCase);

            Add("Abs", 1, 1, a => Num(Math.Abs(NumArg(a, 0))), "Abs(x): absolute value");
            Add("Sqrt", 1, 1, a => Num(Math.Sqrt(NumArg(a, 0))), "Sqrt(x): square root");
            Add("Exp", 1, 1, a => Num(Math.Exp(NumArg(a, 0))), "Exp(x): e^x");
            Add("Log", 1, 1, a => Num(Math.Log(NumArg(a, 0))), "Log(x): natural logarithm");
            Add("Log10", 1, 1, a => Num(Math.Log10(NumArg(a, 0))), "Log10(x): base-10 logarithm");
            Add("Sin", 1, 1, a => Num(Math.Sin(NumArg(a, 0))), "Sin(x): sine (x in radians)");
            Add("Cos", 1, 1, a => Num(Math.Cos(NumArg(a, 0))), "Cos(x): cosine (x in radians)");
            Add("Tan", 1, 1, a => Num(Math.Tan(NumArg(a, 0))), "Tan(x): tangent (x in radians)");
            Add("Asin", 1, 1, a => Num(Math.Asin(NumArg(a, 0))), "Asin(x): arcsine (radians)");
            Add("Acos", 1, 1, a => Num(Math.Acos(NumArg(a, 0))), "Acos(x): arccosine (radians)");
            Add("Atan", 1, 1, a => Num(Math.Atan(NumArg(a, 0))), "Atan(x): arctangent (radians)");
            Add("Sinh", 1, 1, a => Num(Math.Sinh(NumArg(a, 0))), "Sinh(x): hyperbolic sine");
            Add("Cosh", 1, 1, a => Num(Math.Cosh(NumArg(a, 0))), "Cosh(x): hyperbolic cosine");
            Add("Tanh", 1, 1, a => Num(Math.Tanh(NumArg(a, 0))), "Tanh(x): hyperbolic tangent");
            Add("Floor", 1, 1, a => Num(Math.Floor(NumArg(a, 0))), "Floor(x): largest integer <= x");
            Add("Ceil", 1, 1, a => Num(Math.Ceiling(NumArg(a, 0))), "Ceil(x): smallest integer >= x");
            Add("Round", 1, 2, a =>
            {
                double x = NumArg(a, 0);
                if (a.Length == 1) return Num(Math.Round(x));
                int n = IntArg(a, 1, "Round precision");
                return Num(Math.Round(x, n, MidpointRounding.AwayFromZero));
            }, "Round(x[, n]): round x to nearest (optionally n digits)");

            Add("Sign", 1, 1, a => Num(Math.Sign(NumArg(a, 0))), "Sign(x): −1, 0, +1");
            Add("Step", 1, 1, a => Num(NumArg(a, 0) < 0 ? 0 : 1), "Step(x): 0 if x<0 else 1");

            // --- Arithmetic / math (binary) ---
            Add("Pow", 2, 2, a => Num(Math.Pow(NumArg(a, 0), NumArg(a, 1))), "Pow(x,y): x^y");
            Add("Min", 2, 2, a => Num(Math.Min(NumArg(a, 0), NumArg(a, 1))), "Min(x,y): smaller of x,y");
            Add("Max", 2, 2, a => Num(Math.Max(NumArg(a, 0), NumArg(a, 1))), "Max(x,y): larger of x,y");
            Add("Atan2", 2, 2, a => Num(Math.Atan2(NumArg(a, 0), NumArg(a, 1))), "Atan2(y,x): angle of (x,y)");
            Add("Hypot", 2, 2, a => Num(Hypot(NumArg(a, 0), NumArg(a, 1))),
                "Hypot(x,y): sqrt(x^2 + y^2) computed stably");
            Add("Fmod", 2, 2, a => Num(NumArg(a, 0) % NumArg(a, 1)), "Fmod(x,y): floating remainder of x/y");

            // --- Random ---
            Add("Rand", 0, 0, _ => Num(Random.Shared.NextDouble()), "Rand(): uniform random in [0,1]");
            Add("RandInt", 2, 2, a =>
            {
                int lo = IntArg(a, 0, "RandInt low");
                int hi = IntArg(a, 1, "RandInt high");
                if (hi < lo) (lo, hi) = (hi, lo);
                return Num(Random.Shared.Next(lo, hi + 1)); // inclusive hi
            }, "RandInt(lo,hi): integer random in [lo,hi] (inclusive)");

            // --- Strings ---
            Add("Str", 1, 1, a => Text(ToStr(a[0])), "Str(x): convert number/string to string");
            Add("StrCat", 1, int.MaxValue, a =>
            {
                var s = string.Empty;
                foreach (var v in a) s += ToStr(v);
                return Text(s);
            }, "StrCat(a,b,...): concatenate strings");

            Add("Sprintf", 1, int.MaxValue, a =>
            {
                string fmt = ToStr(a[0]);
                object[] objs = new object[a.Length - 1];
                for (int i = 1; i < a.Length; i++)
                    objs[i - 1] = a[i].type == EDataType.String ? (object?)a[i].text ?? "" : a[i].numeric;
                return Text(string.Format(CultureInfo.InvariantCulture, fmt, objs));
            }, "Sprintf(fmt, args...): formatted string (invariant culture)");

            // --- Printing (side effects) ---
            Add("Print", 0, int.MaxValue, a =>
            {
                if (a.Length == 0) Console.WriteLine();
                else if (a.Length == 1) Console.WriteLine(ToStr(a[0]));
                else Console.WriteLine(string.Join(" ", Array.ConvertAll(a, ToStr)));
                return Value.Nothing;
            }, "Print([args...]): write to console; joins multiple args by space");

            Add("Printf", 1, int.MaxValue, a =>
            {
                string fmt = ToStr(a[0]);
                object[] objs = new object[a.Length - 1];
                for (int i = 1; i < a.Length; i++)
                    objs[i - 1] = a[i].type == EDataType.String ? (object?)a[i].text ?? "" : a[i].numeric;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, fmt, objs));
                return Value.Nothing;
            }, "Printf(fmt, args...): formatted console write");

            // --- Environment (optional) ---
            Add("GetEnv", 1, 1, a => Text(Environment.GetEnvironmentVariable(ToStr(a[0])) ?? ""),
                "GetEnv(name): read environment variable (empty if missing)");
            Add("SetEnv", 2, 2, a =>
            {
                Environment.SetEnvironmentVariable(ToStr(a[0]), ToStr(a[1]));
                return Value.Nothing;
            }, "SetEnv(name,value): set environment variable");
        }

        static double Hypot(double x, double y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);

            if (x < y)
            {
                double temp = x;
                x = y;
                y = temp;
            }

            if (x == 0.0) return 0.0;

            double t = y / x;
            return x * Math.Sqrt(1 + t * t);
        }

        static void Add(string name, int minArgs, int maxArgs, Func<Value[], Value> impl, string description)
        {
            _functions.Add(name, new NativeFunction(name, description, Arity(name, minArgs, maxArgs, impl)));
        }

        static Func<Value[], Value> Arity(string name, int min, int max, Func<Value[], Value> impl) =>
            args =>
            {
                if (args == null) throw new ArgumentNullException(nameof(args));
                int n = args.Length;
                if (n < min || n > max)
                    throw new ArgumentException($"Function '{name}' expects {RangeText(min, max)} argument(s), got {n}.");
                return impl(args);
            };

        static string RangeText(int min, int max)
            => (min == max) ? min.ToString()
             : (max == int.MaxValue) ? $"at least {min}"
             : $"{min}..{max}";

        // ------------ value helpers ------------
        private static Value Num(double d) => new Value(d);
        private static Value Text(string s) => new Value(s);

        private static double NumArg(Value[] a, int i)
        {
            if (a[i].type != EDataType.Numeric || double.IsNaN(a[i].numeric))
                throw new ArgumentException($"Argument {i} must be numeric.");
            return a[i].numeric;
        }

        private static int IntArg(Value[] a, int i, string what)
        {
            double d = NumArg(a, i);
            double r = Math.Round(d);
            if (Math.Abs(d - r) > 1e-9)
                throw new ArgumentException($"{what} must be integer (got {d}).");
            if (r > int.MaxValue || r < int.MinValue)
                throw new OverflowException($"{what} out of Int32 range.");
            return (int)r;
        }

        private static string ToStr(Value v)
        {
            if (v.type == EDataType.String) return v.text ?? string.Empty;
            if (v.type == EDataType.Numeric) return v.numeric.ToString("G17", CultureInfo.InvariantCulture);
            return "<none>";
        }
    }
}
