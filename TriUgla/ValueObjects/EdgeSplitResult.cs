namespace TriUgla;

public readonly record struct EdgeSplitResult(
    Edge FirstHalf,
    Edge SecondHalf,
    TopologyChange Change);
