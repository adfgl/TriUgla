namespace TriUgla.Script.Data
{
    public readonly record struct CurveLoopId(int Value)
    {
        public override string ToString() => Value.ToString();

        public static implicit operator int(CurveLoopId id) => id.Value;
        public static implicit operator CurveLoopId(int value) => new(value);
    }
}
