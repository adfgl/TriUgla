namespace TriUgla.Script;

public abstract class ScriptObject
{
}

public sealed class ScriptString(string value) : ScriptObject
{
    public string Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    public override string ToString() => Value;
}
