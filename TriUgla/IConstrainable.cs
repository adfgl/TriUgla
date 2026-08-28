namespace TriUgla;

public interface IConstrainable
{
    bool Constrained { get; }
    void Constrain();
    void Relax();
}