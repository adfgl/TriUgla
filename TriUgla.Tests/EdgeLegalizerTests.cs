namespace TriUgla.Tests;

public class EdgeLegalizerTests
{
    [Fact]
    public void LegalizeProcessesEdgesReturnedByFlip()
    {
        var firstFace = new Face();
        var flippedFace = new Face();
        var followUpFace = new Face();
        var initial = new Edge { Face = firstFace };
        var followUp = new Edge { Face = followUpFace };
        var flipResult = new EdgeFlipResult(
            initial,
            [firstFace, flippedFace],
            [followUp]);
        var flipper = new StubFlipper(initial, flipResult);
        var queue = new Queue<Edge>();
        queue.Enqueue(initial);
        var legalizer = new EdgeLegalizer(flipper);

        EdgeLegalizationResult result = legalizer.Legalize(queue);

        Assert.Empty(queue);
        Assert.Equal(2, flipper.CheckedCount);
        Assert.Equal(1, flipper.FlipCount);
        Assert.Equal(
            new[] { firstFace, flippedFace, followUpFace },
            result.AffectedFaces);
        Assert.Single(result.Flips);
        Assert.Same(initial, result.Flips[0].Edge);
    }

    [Fact]
    public void LegalizeReturnsEmptyResultForEmptyQueue()
    {
        var edge = new Edge();
        var flipper = new StubFlipper(
            edge,
            new EdgeFlipResult(edge, [], []));
        var queue = new Queue<Edge>();
        var legalizer = new EdgeLegalizer(flipper);

        EdgeLegalizationResult result = legalizer.Legalize(queue);

        Assert.Empty(result.AffectedFaces);
        Assert.Empty(result.Flips);
        Assert.Empty(queue);
    }

    sealed class StubFlipper(
        Edge flipTarget,
        EdgeFlipResult flipResult) : IEdgeFlipper
    {
        public int CheckedCount { get; private set; }
        public int FlipCount { get; private set; }

        public bool CanFlip(Edge edge, out bool shouldFlip)
        {
            CheckedCount++;
            shouldFlip = ReferenceEquals(edge, flipTarget);
            return true;
        }

        public EdgeFlipResult Flip(Edge edge)
        {
            Assert.Same(flipTarget, edge);
            FlipCount++;
            return flipResult;
        }
    }
}
