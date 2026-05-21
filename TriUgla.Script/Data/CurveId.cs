namespace TriUgla.Script.Data
{
    public readonly record struct CurveId(int Value)
    {
        public override string ToString() => Value.ToString();

        public static implicit operator int(CurveId id) => id.Value;
        public static implicit operator CurveId(int value) => new(value);
    }
}
