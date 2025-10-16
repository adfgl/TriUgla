using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Nodes;

namespace TriUgla.Parsing
{
    public class NativeFunctions
    {
        public static readonly NativeFunctions ALL = new NativeFunctions();

        readonly Dictionary<string, NativeFunction> _map;

        public IReadOnlyDictionary<string, NativeFunction> All => _map;

        public NativeFunctions(bool includeStandard = true)
        {
            _map = new Dictionary<string, NativeFunction>(StringComparer.OrdinalIgnoreCase);
            if (includeStandard)
                RegisterDefaults();
        }

        public void Register(NativeFunction fn)
        {
            if (_map.ContainsKey(fn.Name))
                throw new InvalidOperationException($"Duplicate native name '{fn.Name}'.");
            _map[fn.Name] = fn;
        }

        public bool TryCall(string name, TuValue[] args, out TuValue result, out string msg)
        {
            result = TuValue.Nothing;
            msg = string.Empty;

            if (!_map.TryGetValue(name, out var fn))
            {
                msg = $"Unknown native function '{name}'.";
                return false;
            }

            return fn.TryExecute(args, out result, out msg);
        }

        public bool Has(string name) => _map.ContainsKey(name);

        public bool TryGet(string name, out NativeFunction fn) => _map.TryGetValue(name, out fn!);

        public List<string> GetHelpLines()
        {
            var lines = new List<string>();
            foreach (var fn in _map.Values.OrderBy(f => f.Name))
            {
                string sig = string.Join(", ", fn.Input.Select(x => x.ToString()));
                lines.Add($"{fn.Name}({sig}) → {fn.Output}");
                if (!string.IsNullOrWhiteSpace(fn.Description))
                    lines.Add("    " + fn.Description);
            }
            return lines;
        }

        public string GetHelpText()
        {
            return string.Join(Environment.NewLine, GetHelpLines());
        }

        private void RegisterDefaults()
        {
            Register(new NativeAbs());
            Register(new NativeAcos());
            Register(new NativeAsin());
            Register(new NativeAtan());
            Register(new NativeAtan2());
            Register(new NativeCeil());
            Register(new NativeCos());
            Register(new NativeCosh());
            Register(new NativeExp());
            Register(new NativeFmod());
            Register(new NativeFloor());
            Register(new NativeHypot());
            Register(new NativeLog());
            Register(new NativeLog10());
            Register(new NativeMax());
            Register(new NativeMin());
            Register(new NativeRand());
            Register(new NativeRound());
            Register(new NativeSin());
            Register(new NativeSinh());
            Register(new NativeSqrt());
            Register(new NativeStep());
            Register(new NativeTan());
            Register(new NativeTanh());

            Register(new NativePrint());
        }
    }


    [Flags]
    public enum EArgKind
    {
        Any = 0,   
        Numeric = 1 << 0,
        String = 1 << 1,
        Tuple = 1 << 2,
        Range = 1 << 3,

        // convenience combos
        Scalar = Numeric | String,
        Iterable = Tuple | Range
    }

    public class ArgSpec
    {
        public string Name { get; }
        public EArgKind Kind { get; }
        public bool Optional { get; }
        public TuValue? DefaultValue { get; }

        public ArgSpec(
            string name,
            EArgKind kind = EArgKind.Any,
            bool optional = false,
            TuValue? defaultValue = null)
        {
            Name = name;
            Kind = kind;
            Optional = optional;
            DefaultValue = defaultValue;
        }

        public bool Matches(TuValue value)
        {
            if (Kind == EArgKind.Any)
                return true;

            EArgKind actual = value.type switch
            {
                EDataType.Numeric => EArgKind.Numeric,
                EDataType.Text => EArgKind.String,
                EDataType.Tuple => EArgKind.Tuple,
                EDataType.Range => EArgKind.Range,
                _ => EArgKind.Any
            };
            return (Kind & actual) != 0;
        }

        public override string ToString()
        {
            string flags = Kind == EArgKind.Any ? "Any" : Kind.ToString();
            return Optional
                ? $"{Name}?:{flags}"
                : $"{Name}:{flags}";
        }
    }

    public abstract class NativeFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ArgSpec[] Input { get; set; } = Array.Empty<ArgSpec>();
        public ArgSpec? Output { get; set; }

        public abstract TuValue Execute(TuValue[] args);

        public bool TryExecute(TuValue[] args, out TuValue result, out string msg)
        {
            result = TuValue.Nothing;
            if (CanRun(args, out msg))
            {
                result = Execute(args);
                return true;
            }
            return false;
        }

        public bool CanRun(TuValue[] args, out string reason)
        {
            reason = string.Empty;

            if (Input is null || Input.Length == 0)
            {
                if (args.Length == 0) return true;
                reason = $"'{Name}': expected 0 arguments, got {args.Length}.";
                return false;
            }

            int required = 0;
            for (int i = 0; i < Input.Length; i++)
                if (!Input[i].Optional) required++;

            if (args.Length < required)
            {
                reason = $"'{Name}': expected at least {required} arguments, got {args.Length}.";
                return false;
            }
            if (args.Length > Input.Length)
            {
                reason = $"'{Name}': expected at most {Input.Length} arguments, got {args.Length}.";
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var spec = Input[i];
                var arg = args[i];
                if (!spec.Matches(arg))
                {
                    reason =
                        $"'{Name}': argument {i + 1} ('{spec.Name}') expected {spec.Kind}, got {arg.type}.";
                    return false;
                }
            }
            return true;
        }


    }

    public sealed class NativeAcos : NativeFunction
    {
        public NativeAcos()
        {
            Name = "Acos";
            Description = "Arc cosine (inverse cosine) of an expression in [-1,1]. Returns a value in [0,Pi].";
            Input = [new ArgSpec("x", EArgKind.Numeric)]; 
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Acos(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeAsin : NativeFunction
    {
        public NativeAsin()
        {
            Name = "Asin";
            Description = "Arc sine (inverse sine) of an expression in [-1,1]. Returns a value in [-Pi/2,Pi/2].";
            Input = [new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Asin(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeAtan : NativeFunction
    {
        public NativeAtan()
        {
            Name = "Atan";
            Description = "Arc tangent (inverse tangent) of expression. Returns a value in [-Pi/2,Pi/2].";
            Input = [new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Atan(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeAtan2 : NativeFunction
    {
        public NativeAtan2()
        {
            Name = "Atan2";
            Description = "Arc tangent (inverse tangent) of the first expression divided by the second. Returns a value in [-Pi,Pi].";
            Input = [
                new ArgSpec("x", EArgKind.Numeric),
                new ArgSpec("y", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double y = args[1].AsNumeric();
            double r = Math.Atan2(x, y);
            return new TuValue(r);
        }
    }

    public sealed class NativeCeil : NativeFunction
    {
        public NativeCeil()
        {
            Name = "Ceil";
            Description = "Rounds expression up to the nearest integer.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Ceiling(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeCos : NativeFunction
    {
        public NativeCos()
        {
            Name = "Cos";
            Description = "Cosine of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Cos(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeCosh : NativeFunction
    {
        public NativeCosh()
        {
            Name = "Cosh";
            Description = "Cosine of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Cosh(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeExp : NativeFunction
    {
        public NativeExp()
        {
            Name = "Exp";
            Description = "Returns the value of e (the base of natural logarithms) raised to the power of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Exp(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeAbs : NativeFunction
    {
        public NativeAbs()
        {
            Name = "Abs";
            Description = "Absolute value of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Abs(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeFmod : NativeFunction
    {
        public NativeFmod()
        {
            Name = "Fmod";
            Description = "Remainder of the division of the first expression by the second, with the sign of the first.";
            Input = [
                new ArgSpec("a", EArgKind.Numeric),
                new ArgSpec("b", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double a = args[0].AsNumeric();
            double b = args[1].AsNumeric();

            double r = double.NaN;
            if (b != 0)
            {
                r = a - b * Math.Truncate(a / b);
            }
            return new TuValue(r);
        }
    }

    public sealed class NativeFloor : NativeFunction
    {
        public NativeFloor()
        {
            Name = "Floor";
            Description = "Remainder of the division of the first expression by the second, with the sign of the first.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Floor(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeHypot : NativeFunction
    {
        public NativeHypot()
        {
            Name = "Hypot";
            Description = "Returns the square root of the sum of the square of its two arguments.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric),
                new ArgSpec("y", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double y = args[1].AsNumeric();
            double r = Math.Sqrt(x * x + y * y);
            return new TuValue(r);
        }
    }

    public sealed class NativeLog : NativeFunction
    {
        public NativeLog()
        {
            Name = "Log";
            Description = "Natural logarithm of expression (expression > 0).";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Log(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeLog10 : NativeFunction
    {
        public NativeLog10()
        {
            Name = "Log10";
            Description = "Base 10 logarithm of expression (expression > 0).";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Log10(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeMin : NativeFunction
    {
        public NativeMin()
        {
            Name = "Min";
            Description = "Minimum of the two arguments.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric),
                new ArgSpec("y", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double y = args[1].AsNumeric();
            double r = Math.Min(x, y);
            return new TuValue(r);
        }
    }

    public sealed class NativeMax : NativeFunction
    {
        public NativeMax()
        {
            Name = "Max";
            Description = "Maximum of the two arguments.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric),
                new ArgSpec("y", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double y = args[1].AsNumeric();
            double r = Math.Max(x, y);
            return new TuValue(r);
        }
    }

    public sealed class NativeRand : NativeFunction
    {
        public NativeRand()
        {
            Name = "Rand";
            Description = "Random number between zero and expression.";
            Input = [
                new ArgSpec("bound", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double bound = args[0].AsNumeric();
            double r;
            if (bound < 0)
            {
                r = bound + Random.Shared.NextDouble() * -bound;
            }
            else
            {
                r = Random.Shared.NextDouble() * bound;
            }
                
            return new TuValue(r);
        }
    }

    public sealed class NativeRound : NativeFunction
    {
        public NativeRound()
        {
            Name = "Round";
            Description = "Rounds expression to the nearest integer.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Round(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeSqrt : NativeFunction
    {
        public NativeSqrt()
        {
            Name = "Sqrt";
            Description = "Square root of expression (expression >= 0).";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Sqrt(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeSin : NativeFunction
    {
        public NativeSin()
        {
            Name = "Sin";
            Description = "Sine of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Sin(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeSinh : NativeFunction
    {
        public NativeSinh()
        {
            Name = "Sinh";
            Description = "Hyperbolic sine of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Sinh(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeStep : NativeFunction
    {
        public NativeStep()
        {
            Name = "Step";
            Description = "Returns 0 if expression is negative, 1 if not.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = x < 0 ? 0 : 1;
            return new TuValue(r);
        }
    }

    public sealed class NativeTan : NativeFunction
    {
        public NativeTan()
        {
            Name = "Tan";
            Description = "Tangent of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Tan(x);
            return new TuValue(r);
        }
    }

    public sealed class NativeTanh : NativeFunction
    {
        public NativeTanh()
        {
            Name = "Tanh";
            Description = "Hyperbolic tangent of expression.";
            Input = [
                new ArgSpec("x", EArgKind.Numeric)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            double x = args[0].AsNumeric();
            double r = Math.Tanh(x);
            return new TuValue(r);
        }
    }

    public sealed class NativePrint : NativeFunction
    {
        public NativePrint()
        {
            Name = "Print";
            Description = "";
            Input = [
                new ArgSpec("x", EArgKind.Any, true)];
            Output = new ArgSpec("result", EArgKind.Numeric);
        }

        public override TuValue Execute(TuValue[] args)
        {
            string r = string.Empty;
            if (args.Length != 0)
            {
                r = args[0].AsString();
            }
            Console.WriteLine(r);
            return new TuValue(r);
        }
    }



}
