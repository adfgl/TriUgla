namespace TriUgla.Tests;

public class TopologyChangeTests
{
    [Fact]
    public void SplitAndFlipResultsContainTopologyChange()
    {
        var face = new Face();
        var edge = new Edge();
        TopologyChange[] changes =
        [
            new FaceSplitResult(new TopologyChange([face], [edge])).Change,
            new EdgeSplitResult(
                edge,
                new Edge(),
                new TopologyChange([face], [edge])).Change,
            new EdgeFlipResult(
                edge,
                new TopologyChange([face], [edge])).Change
        ];

        Assert.All(changes, change =>
        {
            Assert.Same(face, Assert.Single(change.AffectedFaces));
            Assert.Same(edge, Assert.Single(change.EdgesToLegalize));
        });
    }
}
