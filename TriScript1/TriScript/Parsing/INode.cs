namespace TriScript.Parsing
{
    public interface INode
    {
        bool Accept<T>(INodeVisitor<T> visitor, out T? result);
    }
}
