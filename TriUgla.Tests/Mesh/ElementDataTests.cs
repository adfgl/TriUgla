namespace TriUgla.Tests;

public class ElementDataTests
{
    [Fact]
    public void MeshElementsAcceptInheritedDataObjects()
    {
        var data = new TestData("green", 42, new object());
        var node = new Node { Data = data };
        var edge = new Edge { Data = data };
        var face = new Face { Data = data };

        Assert.Same(data, node.Data);
        Assert.Same(data, edge.Data);
        Assert.Same(data, face.Data);
    }

    [Fact]
    public void InterpolatorSupportsClosestTwoAndThreeSourceData()
    {
        var interpolator = new TestInterpolator();
        var first = new TestData("first", 10, null);
        var second = new TestData("second", 20, null);
        var third = new TestData("third", 30, null);

        var closest = Assert.IsType<TestData>(interpolator.From(first));
        var betweenTwo = Assert.IsType<TestData>(interpolator.Between(first, second, 0.25));
        var betweenThree = Assert.IsType<TestData>(interpolator.Between(
            first, second, third, new Barycentric(0.5, 0.25, 0.25)));

        Assert.NotSame(first, closest);
        Assert.Equal(10, closest.Number);
        Assert.Equal(12.5, betweenTwo.Number);
        Assert.Equal(17.5, betweenThree.Number);
    }
}

sealed class TestData(string color, double number, object? link) : ElementData
{
    public string Color { get; } = color;
    public double Number { get; } = number;
    public object? Link { get; } = link;
    public InsertNodeStatus? InsertedStatus { get; private set; }

    public override void AfterInserted(Node node, InsertNodeResult result)
        => InsertedStatus = result.Status;
}

sealed class TestInterpolator : IDataInterpolator
{
    public ElementData From(ElementData closest)
    {
        TestData data = Get(closest);
        return new TestData(data.Color, data.Number, data.Link);
    }

    public ElementData Between(ElementData first, ElementData second, double amount)
    {
        TestData a = Get(first);
        TestData b = Get(second);
        return new TestData(
            amount < 0.5 ? a.Color : b.Color,
            a.Number + (b.Number - a.Number) * amount,
            amount < 0.5 ? a.Link : b.Link);
    }

    public ElementData Between(
        ElementData first,
        ElementData second,
        ElementData third,
        Barycentric weights)
    {
        TestData a = Get(first);
        TestData b = Get(second);
        TestData c = Get(third);
        return new TestData(
            a.Color,
            weights.Interpolate(a.Number, b.Number, c.Number),
            a.Link);
    }

    static TestData Get(ElementData data) => (TestData)data;
}
