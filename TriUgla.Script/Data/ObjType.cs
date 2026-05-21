namespace TriUgla.Script.Data
{
    public sealed record ObjType(DataKind Kind, ObjType? Element = null)
    {
        public static readonly ObjType Undefined = new(DataKind.Undefined);
        public static readonly ObjType Integer = new(DataKind.Integer);
        public static readonly ObjType Double = new(DataKind.Double);
        public static readonly ObjType Boolean = new(DataKind.Boolean);
        public static readonly ObjType String = new(DataKind.String);
        public static readonly ObjType Range = new(DataKind.Range);

        public static ObjType ListOf(ObjType element)
            => new(DataKind.List, element);

        public bool IsNumber => Kind is DataKind.Integer or DataKind.Double;
        public bool IsList => Kind == DataKind.List;

        public override string ToString()
        {
            return Kind == DataKind.List
                ? $"List<{Element}>"
                : Kind.ToString();
        }
    }
}
