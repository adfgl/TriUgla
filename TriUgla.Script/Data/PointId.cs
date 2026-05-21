namespace TriUgla.Script.Data
{
    public readonly record struct PointId(int Value)
    {
        public override string ToString() => Value.ToString();

        public static implicit operator int(PointId id) => id.Value;
        public static implicit operator PointId(int value) => new(value);
    }
}
