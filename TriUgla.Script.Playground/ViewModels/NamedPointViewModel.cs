namespace TriUgla.Script.Playground.ViewModels;

public sealed record NamedPointViewModel(
    string Name,
    int Tag,
    double X,
    double Y,
    double Z,
    double? MeshSize);
