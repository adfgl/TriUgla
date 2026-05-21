using TriUgla.Script.Data.Geometry;

namespace TriUgla.Script.Data
{
    public sealed class GeometryContext
    {
        public Dictionary<PointId, ObjPoint> Points { get; } = [];
        public Dictionary<CurveId, ObjCurve> Curves { get; } = [];
        public Dictionary<CurveLoopId, ObjCurveLoop> CurveLoops { get; } = [];
        public Dictionary<SurfaceId, ObjPlaneSurface> PlaneSurfaces { get; } = [];

        public List<ObjPhysicalGroup> PhysicalGroups { get; } = [];

        public List<ObjTransfiniteCurve> TransfiniteCurves { get; } = [];
        public List<ObjTransfiniteSurface> TransfiniteSurfaces { get; } = [];
        public List<ObjRecombineSurface> RecombineSurfaces { get; } = [];

        public List<ObjEmbedConstraint> Embeds { get; } = [];

        public void Add(ObjPoint point)
        {
            Points[point.Id] = point;
        }

        public void Add(ObjCurve curve)
        {
            Curves[curve.Id] = curve;
        }

        public void Add(ObjCurveLoop loop)
        {
            CurveLoops[loop.Id] = loop;
        }

        public void Add(ObjPlaneSurface surface)
        {
            PlaneSurfaces[surface.Id] = surface;
        }

        public void Add(ObjPhysicalGroup group)
        {
            PhysicalGroups.Add(group);
        }

        public void Add(ObjTransfiniteCurve transfinite)
        {
            TransfiniteCurves.Add(transfinite);
        }

        public void Add(ObjTransfiniteSurface transfinite)
        {
            TransfiniteSurfaces.Add(transfinite);
        }

        public void Add(ObjRecombineSurface recombine)
        {
            RecombineSurfaces.Add(recombine);
        }

        public void Add(ObjEmbedConstraint embed)
        {
            Embeds.Add(embed);
        }
    }
}
