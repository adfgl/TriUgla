using System.Globalization;

namespace TriUgla.Script;

public readonly record struct Value
{
    readonly double _number;
    readonly ScriptObject? _object;

    public ValueKind Kind { get; }
    public bool IsNumber => Kind == ValueKind.Number;
    public bool IsObject => Kind == ValueKind.Object;

    public double Number => IsNumber
        ? _number
        : throw new InvalidOperationException("Value does not contain a number.");

    public ScriptObject Object => IsObject
        ? _object!
        : throw new InvalidOperationException("Value does not contain an object.");

    public Value(double number)
    {
        _number = number;
        _object = null;
        Kind = ValueKind.Number;
    }

    public Value(ScriptObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _number = default;
        _object = value;
        Kind = ValueKind.Object;
    }

    public T As<T>() where T : ScriptObject
        => Object as T ?? throw new InvalidCastException(
            $"Script object is not a {typeof(T).Name}.");

    public static implicit operator Value(double value) => new(value);

    public static implicit operator Value(ScriptObject value) => new(value);

    public static implicit operator Value(string value) => new(new ScriptString(value));

    public override string ToString()
        => IsNumber
            ? _number.ToString(CultureInfo.InvariantCulture)
            : Object.ToString() ?? string.Empty;
}
