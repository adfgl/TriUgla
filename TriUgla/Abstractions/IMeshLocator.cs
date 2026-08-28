namespace TriUgla;

public interface IMeshLocator
{
    LocateResult Locate(Vec2 point, Face? from = null);
}
