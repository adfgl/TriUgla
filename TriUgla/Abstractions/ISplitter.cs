namespace TriUgla;

public interface ISplitter
{
    FaceSplitResult Split(Face target, Node node);

    EdgeSplitResult Split(Edge target, Node node);
}
