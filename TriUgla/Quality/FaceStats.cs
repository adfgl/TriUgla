namespace TriUgla;

public readonly record struct FaceStats(
    double SignedArea,
    double MinLen2,
    double MaxLen2,
    double AvgVertexArea,
    double Cx,
    double Cy);
