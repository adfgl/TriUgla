namespace TriUgla;

public sealed class ConstraintPoint : INamable
{
    Node _node;

    public ConstraintPoint(Node node, string? name = null)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
        Name = name;
    }

    public string? Name { get; set; }

    public Node Node
    {
        get => _node;
        set => _node = value ?? throw new ArgumentNullException(nameof(value));
    }
}
