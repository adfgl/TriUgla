namespace TriUgla.Tests;

public class MeshMetricTests
{
    [Fact]
    public void SetTracksCountAverageAndExtrema()
    {
        var first = new Edge();
        var second = new Edge();
        var third = new Edge();
        var metric = new MeshMetric<Edge>();

        metric.Set(first, 4);
        metric.Set(second, 1);
        metric.Set(third, 7);

        Assert.Equal(3, metric.Count);
        Assert.Equal(4, metric.Average, 12);
        Assert.Equal(1, metric.Min);
        Assert.Same(second, metric.MinElement);
        Assert.Equal(7, metric.Max);
        Assert.Same(third, metric.MaxElement);
    }

    [Fact]
    public void EmptyMetricFormatsAsNoData()
        => Assert.Equal("no data", new MeshMetric<Face>().ToString());

    [Fact]
    public void MetricFormatsValuesAndUnit()
    {
        var metric = new MeshMetric<Edge>();
        metric.Set(new Edge(), 1.25);
        metric.Set(new Edge(), 2.75);

        Assert.Equal(
            $"min: {1.25:F2} °, avg: {2d:F2} °, max: {2.75:F2} ° (n=2)",
            metric.ToString("°"));
    }

    [Fact]
    public void MeshMetricsFormatsSummary()
    {
        string text = new MeshMetrics().ToString();

        Assert.Contains("Mesh metrics", text);
        Assert.Contains("Angle       : no data", text);
        Assert.Contains("Edge length : no data", text);
        Assert.Contains("Face area   : no data", text);
    }

    [Fact]
    public void SetRejectsNullElement()
    {
        var metric = new MeshMetric<Edge>();

        Assert.Throws<ArgumentNullException>(() => metric.Set(null!, 1));
    }
}
