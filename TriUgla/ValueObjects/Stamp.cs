namespace TriUgla;

/// <summary>
/// Identifies a traversal.
/// </summary>
public readonly record struct Stamp(uint Value)
{
    public static Stamp None => default;
}
