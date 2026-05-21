namespace TriUgla.Script
{
    public enum Keyword
    {
        None,

        // control flow
        If,
        Else,
        ElseIf,
        EndIf,

        For,
        In,
        EndFor,

        While,
        EndWhile,

        Break,
        Continue,

        // declarations
        Point,
        Line,
        CurveLoop,
        PlaneSurface,

        // mesh operations
        Mesh,
        Refine,
        Recombine,

        // utility
        Include,
        Printf,

        // literals
        True,
        False
    }
}
