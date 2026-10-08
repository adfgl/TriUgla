namespace TriUgla;

public readonly record struct QuadMeshConstraint(
    int A,
    int B,
    int FeatureConstraints,
    int BoundaryConstraints);
