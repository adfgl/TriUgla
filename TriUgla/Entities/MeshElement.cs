namespace TriUgla;

/// <summary>
/// Provides stamp-based visitation for mesh elements.
/// </summary>
public class MeshElement : IVisitable
{
    Stamp _stamp = Stamp.None;

    public ElementData? Data { get; set; }

    public bool TryVisit(Stamp stamp)
    {
        if (stamp == Stamp.None)
        {
            throw new ArgumentException("None is not a valid visit stamp.", nameof(stamp));
        }

        if (_stamp == stamp) return false;

        _stamp = stamp;
        return true;
    }

    internal void ResetStamp() => _stamp = Stamp.None;
}
