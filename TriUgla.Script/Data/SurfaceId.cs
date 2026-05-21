namespace TriUgla.Script.Data
{
    public readonly record struct SurfaceId(int Value)
    {
        public override string ToString() => Value.ToString();

        public static implicit operator int(SurfaceId id) => id.Value;
        public static implicit operator SurfaceId(int value) => new(value);
    }
}
