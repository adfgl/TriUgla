namespace TriUgla.Tests;

public class SegmentQueueTests
{
    [Fact]
    public void DequeuesSegmentsInInsertionOrder()
    {
        var first = new Node();
        var second = new Node();
        var third = new Node();
        var queue = new SegmentQueue();

        queue.Enqueue(first, second);
        queue.Enqueue(second, third);

        Assert.True(queue.TryDequeue(out Node start, out Node end));
        Assert.Same(first, start);
        Assert.Same(second, end);
        Assert.True(queue.TryDequeue(out start, out end));
        Assert.Same(second, start);
        Assert.Same(third, end);
        Assert.False(queue.TryDequeue(out _, out _));
    }

    [Fact]
    public void Enqueue_IgnoresSegmentWithSameNodeAtBothEnds()
    {
        var node = new Node();
        var queue = new SegmentQueue();

        queue.Enqueue(node, node);

        Assert.False(queue.TryDequeue(out _, out _));
    }
}
