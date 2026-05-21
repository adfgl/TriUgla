using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Scanning
{
    public static class Keywords
    {
        public static readonly Dictionary<string, Keyword> Source = new()
        {
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

            ["True"] = Keyword.True,
            ["False"] = Keyword.False,

            ["Point"] = Keyword.Point,
            ["Line"] = Keyword.Line,
            ["CurveLoop"] = Keyword.CurveLoop,
            ["PlaneSurface"] = Keyword.PlaneSurface,
        };
    }
}
