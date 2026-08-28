namespace TriUgla;

/// <summary>
/// Supports stamp-based traversal.
/// </summary>
public interface IVisitable
{
    /// <summary>
    /// Applies the stamp if it has not already been applied.
    /// </summary>
    bool TryVisit(Stamp stamp);
}
