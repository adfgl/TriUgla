namespace TriUgla.Script.Data
{
    public readonly record struct PhysicalGroupId(int Value)
    {
        public override string ToString() => Value.ToString();

        public static implicit operator int(PhysicalGroupId id) => id.Value;
        public static implicit operator PhysicalGroupId(int value) => new(value);
    }
}
