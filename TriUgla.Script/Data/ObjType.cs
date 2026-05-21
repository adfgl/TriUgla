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

        public static readonly ObjType Point = new(DataKind.Point);
        public static readonly ObjType Line = new(DataKind.Line);
        public static readonly ObjType Circle = new(DataKind.Circle);
        public static readonly ObjType Ellipse = new(DataKind.Ellipse);
        public static readonly ObjType Spline = new(DataKind.Spline);
        public static readonly ObjType BSpline = new(DataKind.BSpline);
        public static readonly ObjType Bezier = new(DataKind.Bezier);
        public static readonly ObjType CurveLoop = new(DataKind.CurveLoop);
        public static readonly ObjType PlaneSurface = new(DataKind.PlaneSurface);
        public static readonly ObjType MeshCommand = new(DataKind.MeshCommand);
        public static readonly ObjType MeshOption = new(DataKind.MeshOption);

        public static ObjType ListOf(ObjType element) => new(DataKind.List, element);

        public bool IsNumber => Kind is DataKind.Integer or DataKind.Double;
        public bool IsList => Kind == DataKind.List;
        public bool IsGeometry => Kind is
            DataKind.Point or
            DataKind.Line or
            DataKind.Circle or
            DataKind.Ellipse or
            DataKind.Spline or
            DataKind.BSpline or
            DataKind.Bezier or
            DataKind.CurveLoop or
            DataKind.PlaneSurface;

        public bool IsVector =>
            Kind == DataKind.List &&
            Element is not null &&
            Element.IsNumber;

        public bool IsMatrix =>
            Kind == DataKind.List &&
            Element is not null &&
            Element.IsVector;

        public static ObjType VectorOf(ObjType element)
            => ListOf(element);

        public static ObjType MatrixOf(ObjType element)
            => ListOf(ListOf(element));

        public override string ToString()
            => Kind == DataKind.List ? $"List<{Element}>" : Kind.ToString();
    }
}
