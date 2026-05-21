using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Scanning
{
    public static class Keywords
    {
        public static readonly Dictionary<string, Keyword> Source = new Dictionary<string, Keyword>(StringComparer.OrdinalIgnoreCase)
        {
            // boolean operators
            ["Or"] = Keyword.Or,
            ["Not"] = Keyword.Not,
            ["And"] = Keyword.And,
            ["Is"] = Keyword.Is,

            // control flow
            ["If"] = Keyword.If,
            ["Else"] = Keyword.Else,
            ["ElseIf"] = Keyword.ElseIf,
            ["EndIf"] = Keyword.EndIf,

            ["For"] = Keyword.For,
            ["In"] = Keyword.In,
            ["EndFor"] = Keyword.EndFor,

            ["While"] = Keyword.While,
            ["EndWhile"] = Keyword.EndWhile,

            ["Break"] = Keyword.Break,
            ["Continue"] = Keyword.Continue,

            ["Return"] = Keyword.Return,

            // literals
            ["True"] = Keyword.True,
            ["False"] = Keyword.False,

            // geometry
            ["Point"] = Keyword.Point,

            ["Line"] = Keyword.Line,
            ["Circle"] = Keyword.Circle,
            ["Ellipse"] = Keyword.Ellipse,

            ["Spline"] = Keyword.Spline,
            ["BSpline"] = Keyword.BSpline,
            ["Bezier"] = Keyword.Bezier,

            ["Curve"] = Keyword.Curve,
            ["Loop"] = Keyword.Loop,

            ["Plane"] = Keyword.Plane,
            ["Surface"] = Keyword.Surface,

            ["Physical"] = Keyword.Physical,

            // transfinite
            ["Transfinite"] = Keyword.Transfinite,
            ["Using"] = Keyword.Using,
            ["Progression"] = Keyword.Progression,

            // embedding
            ["Points"] = Keyword.Points,
            ["Curves"] = Keyword.Curves,
            ["Surfaces"] = Keyword.Surfaces,

            // mesh
            ["Mesh"] = Keyword.Mesh,
            ["Refine"] = Keyword.Refine,
            ["Recombine"] = Keyword.Recombine,

            ["Algorithm"] = Keyword.Algorithm,
            ["Optimize"] = Keyword.Optimize,
            ["Smooth"] = Keyword.Smooth,

            // commands
            ["Coherence"] = Keyword.Coherence,
            ["RenumberMeshNodes"] = Keyword.RenumberMeshNodes,
            ["RenumberMeshElements"] = Keyword.RenumberMeshElements,

            // utility
            ["Include"] = Keyword.Include,
            ["Printf"] = Keyword.Printf,

            // special
            ["DefineConstant"] = Keyword.DefineConstant,
            ["SetFactory"] = Keyword.SetFactory,
        };
    }
}
