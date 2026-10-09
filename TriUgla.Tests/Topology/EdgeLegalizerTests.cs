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
            new TopologyChange(
                [firstFace, flippedFace],
                [followUp]));
        int checkedCount = 0;
        int flipCount = 0;
        var queue = new Queue<Edge>();
        queue.Enqueue(initial);
        var legalizer = new EdgeLegalizer(
            edge => { checkedCount++; return ReferenceEquals(edge, initial); },
            edge => { Assert.Same(initial, edge); flipCount++; return flipResult; });

        EdgeLegalizationResult result = legalizer.Legalize(queue);

        Assert.Empty(queue);
        Assert.Equal(2, checkedCount);
        Assert.Equal(1, flipCount);
        Assert.Equal(
            new[] { firstFace, flippedFace, followUpFace },
            result.AffectedFaces);
        Assert.Single(result.Flips);
        Assert.Same(initial, result.Flips[0].Edge);
    }

    [Fact]
    public void LegalizeReturnsEmptyResultForEmptyQueue()
    {
        var queue = new Queue<Edge>();
        var legalizer = new EdgeLegalizer(
            _ => false,
            _ => throw new InvalidOperationException("No edge should be flipped."));

        EdgeLegalizationResult result = legalizer.Legalize(queue);

        Assert.Empty(result.AffectedFaces);
        Assert.Empty(result.Flips);
        Assert.Empty(queue);
    }
}
