namespace TriUgla.Script;

public abstract class ScriptObject
{
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
