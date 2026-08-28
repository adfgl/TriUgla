namespace TriUgla;

public interface IEdgeFlipper
{
    EdgeFlipResult Flip(Edge edge);

    bool CanFlip(Edge edge, out bool shouldFlip);
}
