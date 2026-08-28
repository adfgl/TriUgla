namespace TriUgla;

public sealed class Node : IConstrainable
{
    int _constraints = 0;

    public Vec2 Position;
    public Edge Edge = null!;

    public bool Constrained => _constraints > 0;

    public void Constrain() => _constraints++;

    public void Relax() 
    {
        if (Constrained)
        {
            _constraints--;
        }
    }
}
