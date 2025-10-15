namespace TriUgla.Parsing.Nodes
{
    public interface IParsableNode<T>
    {
        static abstract T Parse(Parser p);
    }
}
