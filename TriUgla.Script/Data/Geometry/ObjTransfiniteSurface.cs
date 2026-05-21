using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjTransfiniteSurface(
    IReadOnlyList<SurfaceId> surfaceIds,
    IReadOnlyList<PointId> cornerIds) : Obj
    {
        public IReadOnlyList<SurfaceId> SurfaceIds { get; } = surfaceIds;

        public IReadOnlyList<PointId> CornerIds { get; } = cornerIds;

        public override DataKind Kind => DataKind.TransfiniteSurface;

        public override string ToString()
            => $"Transfinite Surface{{{string.Join(", ", SurfaceIds)}}}";
    }
}
