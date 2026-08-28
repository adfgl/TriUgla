namespace TriUgla;

public interface IDataInterpolator
{
    ElementData From(ElementData closest);

    ElementData Between(
        ElementData first,
        ElementData second,
        double amount);

    ElementData Between(
        ElementData first,
        ElementData second,
        ElementData third,
        Barycentric weights);
}
