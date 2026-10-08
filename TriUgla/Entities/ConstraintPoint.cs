namespace TriUgla;

public sealed class ConstraintPoint(Node node, string? name = null) : INamable
{
    Node _node = node ?? throw new ArgumentNullException(nameof(node));

    public string? Name { get; set; } = name;

    public Node Node
    {
        get => _node;
        set => _node = value ?? throw new ArgumentNullException(nameof(value));
    }
}
