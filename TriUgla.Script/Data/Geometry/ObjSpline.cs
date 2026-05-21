using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjSpline(
    CurveId id,
    IReadOnlyList<PointId> pointIds)
    : ObjCurve(id, pointIds)
    {
        public override DataKind Kind => DataKind.Spline;

        public override string ToString()
            => $"Spline({Id}) = {{{string.Join(", ", PointIds)}}}";
    }
}
