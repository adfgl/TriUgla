namespace TriUgla;

public interface INodeFactory
{
    Node Create(Vec2 position, LocateResult location);
}
