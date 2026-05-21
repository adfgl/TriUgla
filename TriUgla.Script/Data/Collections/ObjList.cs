namespace TriUgla.Script.Data.Collections
{
    public sealed class ObjList(List<Value> values) : Obj, IObjEnumerable
    {
        public List<Value> Values { get; } = values ?? [];

        public override DataKind Kind => DataKind.List;

        public IEnumerable<Value> Enumerate()
            => Values;

        public override string ToString()
            => "{" + string.Join(", ", Values) + "}";
    }
}
