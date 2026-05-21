using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjTransfiniteCurve(
    IReadOnlyList<CurveId> curveIds,
    int divisions,
    double progression) : Obj
    {
        public IReadOnlyList<CurveId> CurveIds { get; } = curveIds;

        public int Divisions { get; } = divisions;

        public double Progression { get; } = progression;

        public override DataKind Kind => DataKind.TransfiniteCurve;

        public override string ToString()
            => $"Transfinite Curve{{{string.Join(", ", CurveIds)}}} = {Divisions} Using Progression {Progression}";
    }
}
