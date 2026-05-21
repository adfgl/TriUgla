namespace TriUgla.Script.Scanning
{
    public enum Keyword
    {
        None,

        // boolean operators
        Or,
        Not,
        And,
        Is,

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

        Return,

        // literals
        True,
        False,

        // declarations
        Point,

        Line,
        Circle,
        Ellipse,

        Spline,
        BSpline,
        Bezier,

        Curve,
        Loop,

        Plane,
        Surface,

        Physical,

        // transfinite
        Transfinite,
        Using,
        Progression,

        // embedding
        Points,
        Curves,
        Surfaces,

        // mesh
        Mesh,
        Refine,
        Recombine,

        Algorithm,
        Optimize,
        Smooth,

        // commands
        Coherence,
        RenumberMeshNodes,
        RenumberMeshElements,

        // utility
        Include,
        Printf,

        // special
        DefineConstant,
        SetFactory
    }
}
