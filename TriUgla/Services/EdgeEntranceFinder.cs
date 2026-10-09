namespace TriUgla;

public sealed class EdgeEntranceFinder(IGeometry geometry)
{
    public Edge? Find(Node start, Node end)
    {
        if (start.Edge is null) return null;

        Edge first = start.Edge;
        Edge current = first;

        do
        {
            Edge previous = current.Prev;
            EOrientaiton currentSide = geometry.Orient(current, end.Position);
            EOrientaiton previousSide = geometry.Orient(previous, end.Position);

            if (OnCurrentEdge(currentSide, previousSide) ||
                InsideCurrentFace(currentSide, previousSide))
                return current;

            Edge? next = previous.Twin;
            if (OnPreviousEdge(currentSide, previousSide)) return next;
            if (next is null) return null;

            current = next;
        }
        while (!ReferenceEquals(first, current));

        return null;
    }

    static bool OnCurrentEdge(EOrientaiton currentSide, EOrientaiton previousSide)
        => currentSide == EOrientaiton.Collinear &&
           previousSide == EOrientaiton.Counterclockwise;

    static bool InsideCurrentFace(EOrientaiton currentSide, EOrientaiton previousSide)
        => currentSide == EOrientaiton.Counterclockwise &&
           previousSide == EOrientaiton.Counterclockwise;

    static bool OnPreviousEdge(EOrientaiton currentSide, EOrientaiton previousSide)
        => currentSide == EOrientaiton.Counterclockwise &&
           previousSide == EOrientaiton.Collinear;
}
