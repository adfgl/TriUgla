namespace TriUgla;

public readonly record struct EdgeSplitResult(
    Edge FirstHalf,
    Edge SecondHalf,
    Face CAe,
    Face BCe,
    Face ADe,
    Face DBe);
