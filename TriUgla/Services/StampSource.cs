namespace TriUgla;

/// <summary>
/// Issues sequential traversal stamps.
/// </summary>
sealed class StampSource(Stamp current)
{
    uint _value = current.Value;

    public StampSource() : this(Stamp.None)
    {
    }

    /// <summary>
    /// Tries to issue the next stamp without overflowing.
    /// </summary>
    public bool TryNext(out Stamp stamp)
    {
        if (_value == uint.MaxValue)
        {
            stamp = Stamp.None;
            return false;
        }

        stamp = new Stamp(++_value);
        return true;
    }

    public void Reset() => _value = Stamp.None.Value;
}
