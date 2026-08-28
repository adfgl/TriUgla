namespace TriUgla;

/// <summary>
/// Tracks visits during traversal.
/// </summary>
public readonly struct Stamp(uint value)
{
    public readonly static Stamp Zero = new Stamp(0);

    readonly uint _value = value;

    /// <summary>
    /// Tries to advance without overflowing.
    /// </summary>
    public bool TryNext(out Stamp next)
    {
        if (_value == uint.MaxValue)
        {
            next = default;
            return false;
        }

        next = new Stamp(_value + 1);
        return true;
    }

    public bool Equals(Stamp other)
        => _value == other._value;
}
