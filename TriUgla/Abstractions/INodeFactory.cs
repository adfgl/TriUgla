namespace TriUgla;

public interface INodeFactory
{
    Node Create(
        Vec2 position,
        LocateResult location,
        ElementData? incomingData = null);

    ElementData? CreateData(
        Vec2 position,
        LocateResult location,
        ElementData? incomingData = null);
}
