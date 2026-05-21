using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjCurveLoop(
    CurveLoopId id,
    IReadOnlyList<OrientedCurveId> curveIds)
    : ObjEntity<CurveLoopId>(id)
    {
        public IReadOnlyList<OrientedCurveId> CurveIds { get; } = curveIds;

        public override DataKind Kind => DataKind.CurveLoop;

        public override string ToString()
            => $"Curve Loop({Id}) = {{{string.Join(", ", CurveIds)}}}";
    }
}
