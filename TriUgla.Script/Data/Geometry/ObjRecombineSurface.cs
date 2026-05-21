using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjRecombineSurface(
    IReadOnlyList<SurfaceId> surfaceIds) : Obj
    {
        public IReadOnlyList<SurfaceId> SurfaceIds { get; } = surfaceIds;

        public override DataKind Kind => DataKind.RecombineSurface;

        public override string ToString()
            => $"Recombine Surface{{{string.Join(", ", SurfaceIds)}}}";
    }
}
