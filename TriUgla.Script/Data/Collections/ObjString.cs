namespace TriUgla.Script.Data.Collections
{
    public sealed class ObjString(string value) : Obj, IObjEnumerable
    {
        public string Content { get; } = value ?? string.Empty;

        public override DataKind Kind => DataKind.String;

        public override string ToString() => Content;

        public IEnumerable<Value> Enumerate()
        {
            foreach (char v in Content)
            {
                yield return Value.FromString($"{v}");
            }
        }
    }
}
