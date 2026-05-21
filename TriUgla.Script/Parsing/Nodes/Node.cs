namespace TriUgla.Script.Parsing.Nodes
{

    public abstract class Node
    {
        public abstract T Accept<T>(INodeVisitor<T> visitor);
    }
}
