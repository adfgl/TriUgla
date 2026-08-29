namespace TriUgla;

public interface INodeInserter
{
    InsertNodeResult Insert(Vec2 position, Face? from = null);
}
