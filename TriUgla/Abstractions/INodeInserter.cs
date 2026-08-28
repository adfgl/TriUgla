namespace TriUgla;

public interface INodeInserter
{
    InsertNodeResult Insert(
        Vec2 position,
        ElementData? incomingData = null,
        Face? from = null);
}
