namespace TriUgla.Script;

public abstract class ScriptObject
{
    public virtual IReadOnlyList<string> PropertyNames => [];

    public virtual Value GetProperty(string name)
        => throw new InvalidOperationException(
            $"{GetType().Name} does not contain a property definition named '{name}'.");

    public virtual void SetProperty(string name, Value value)
        => throw new InvalidOperationException(
            $"{GetType().Name} does not contain a writable property definition named '{name}'.");
}

public sealed class ScriptString(string value) : ScriptObject
{
    public string Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    public override string ToString() => Value;
}

public sealed class ScriptList(IEnumerable<Value> items) : ScriptObject
{
    public IReadOnlyList<Value> Items { get; } = items.ToArray();

    public override string ToString() => $"{{{string.Join(", ", Items)}}}";
}
