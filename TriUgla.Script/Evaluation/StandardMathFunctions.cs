using System.Collections.Frozen;

namespace TriUgla.Script;

public static class StandardMathFunctions
{
    public static IReadOnlyDictionary<string, Func<IReadOnlyList<Value>, Value>> All { get; } =
        new Dictionary<string, Func<IReadOnlyList<Value>, Value>>(StringComparer.Ordinal)
        {
            ["Acos"] = arguments => Unary("Acos", arguments, Math.Acos, value => value is >= -1d and <= 1d, "use a value between -1 and 1"),
            ["Asin"] = arguments => Unary("Asin", arguments, Math.Asin, value => value is >= -1d and <= 1d, "use a value between -1 and 1"),
            ["Atan"] = arguments => Unary("Atan", arguments, Math.Atan),
            ["Atan2"] = arguments => Binary("Atan2", arguments, Math.Atan2),
            ["Ceil"] = arguments => Unary("Ceil", arguments, Math.Ceiling),
            ["Cos"] = arguments => Unary("Cos", arguments, Math.Cos),
            ["Cosh"] = arguments => Unary("Cosh", arguments, Math.Cosh),
            ["Exp"] = arguments => Unary("Exp", arguments, Math.Exp),
            ["Fabs"] = arguments => Unary("Fabs", arguments, Math.Abs),
            ["Fmod"] = arguments => Modulo("Fmod", arguments),
            ["Floor"] = arguments => Unary("Floor", arguments, Math.Floor),
            ["Hypot"] = arguments => Binary("Hypot", arguments, double.Hypot),
            ["Log"] = arguments => Unary("Log", arguments, Math.Log, value => value > 0d, "use a value greater than zero"),
            ["Log10"] = arguments => Unary("Log10", arguments, Math.Log10, value => value > 0d, "use a value greater than zero"),
            ["Max"] = arguments => Variadic("Max", arguments, values => values.Max()),
            ["Min"] = arguments => Variadic("Min", arguments, values => values.Min()),
            ["Modulo"] = arguments => Modulo("Modulo", arguments),
            ["Rand"] = arguments => Unary("Rand", arguments, value => Random.Shared.NextDouble() * value),
            ["Round"] = arguments => Unary("Round", arguments, value => Math.Round(value, MidpointRounding.AwayFromZero)),
            ["Sqrt"] = arguments => Unary("Sqrt", arguments, Math.Sqrt, value => value >= 0d, "use a value greater than or equal to zero"),
            ["Sin"] = arguments => Unary("Sin", arguments, Math.Sin),
            ["Sinh"] = arguments => Unary("Sinh", arguments, Math.Sinh),
            ["Step"] = arguments => Unary("Step", arguments, value => value < 0d ? 0d : 1d),
            ["Tan"] = arguments => Unary("Tan", arguments, Math.Tan),
            ["Tanh"] = arguments => Unary("Tanh", arguments, Math.Tanh)
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlySet<string> Names { get; } =
        All.Keys.Append("Print").ToFrozenSet(StringComparer.Ordinal);

    static Value Unary(
        string name,
        IReadOnlyList<Value> arguments,
        Func<double, double> operation,
        Func<double, bool>? domain = null,
        string? domainHint = null)
    {
        RequireCount(name, arguments, 1);
        double value = RequireNumber(name, arguments[0], 1);
        if (domain is not null && !domain(value))
        {
            throw new InvalidOperationException(
                $"Function '{name}' cannot accept {value}. Hint: {domainHint}.");
        }

        return operation(value);
    }

    static Value Binary(
        string name,
        IReadOnlyList<Value> arguments,
        Func<double, double, double> operation)
    {
        RequireCount(name, arguments, 2);
        double first = RequireNumber(name, arguments[0], 1);
        double second = RequireNumber(name, arguments[1], 2);
        return operation(first, second);
    }

    static Value Modulo(string name, IReadOnlyList<Value> arguments)
    {
        RequireCount(name, arguments, 2);
        double first = RequireNumber(name, arguments[0], 1);
        double second = RequireNumber(name, arguments[1], 2);
        if (second == 0d)
        {
            throw new InvalidOperationException(
                $"Function '{name}' cannot divide by zero. Hint: make argument 2 a non-zero number.");
        }

        return first % second;
    }

    static Value Variadic(
        string name,
        IReadOnlyList<Value> arguments,
        Func<IEnumerable<double>, double> operation)
    {
        if (arguments.Count == 0)
        {
            throw new InvalidOperationException(
                $"Function '{name}' expects at least 1 argument, but received 0. " +
                "Hint: provide one or more numeric expressions.");
        }

        double[] values = arguments
            .Select((argument, index) => RequireNumber(name, argument, index + 1))
            .ToArray();
        return operation(values);
    }

    static void RequireCount(string name, IReadOnlyList<Value> arguments, int expected)
    {
        if (arguments.Count != expected)
        {
            throw new InvalidOperationException(
                $"Function '{name}' expects exactly {expected} argument{(expected == 1 ? string.Empty : "s")}, " +
                $"but received {arguments.Count}. Hint: call it with {expected} numeric argument{(expected == 1 ? string.Empty : "s")}.");
        }
    }

    static double RequireNumber(string name, Value value, int position)
    {
        if (!value.IsNumber)
        {
            throw new InvalidOperationException(
                $"Function '{name}' requires argument {position} to be a number. " +
                $"Hint: replace the {Describe(value)} with a numeric expression.");
        }

        return value.Number;
    }

    static string Describe(Value value)
        => value.Object switch
        {
            ScriptString => "string",
            ScriptList => "list",
            _ => $"object of type {value.Object.GetType().Name}"
        };
}
