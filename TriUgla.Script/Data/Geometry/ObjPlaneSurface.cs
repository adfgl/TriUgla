using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjPlaneSurface(
    SurfaceId id,
    IReadOnlyList<CurveLoopId> loopIds)
    : ObjEntity<SurfaceId>(id)
    {
        public IReadOnlyList<CurveLoopId> LoopIds { get; } = loopIds;

        public override DataKind Kind => DataKind.PlaneSurface;

        public override string ToString()
            => $"Plane Surface({Id}) = {{{string.Join(", ", LoopIds)}}}";
    }
}
