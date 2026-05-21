using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjBezier(
    CurveId id,
    IReadOnlyList<PointId> pointIds)
    : ObjCurve(id, pointIds)
    {
        public override DataKind Kind => DataKind.Bezier;

        public override string ToString()
            => $"Bezier({Id}) = {{{string.Join(", ", PointIds)}}}";
    }
}
