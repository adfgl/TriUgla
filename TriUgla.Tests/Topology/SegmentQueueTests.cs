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

        Assert.True(queue.TryEnqueue(first, second));
        Assert.True(queue.TryEnqueue(second, third));

        Assert.True(queue.TryDequeue(out Node start, out Node end));
        Assert.Same(first, start);
        Assert.Same(second, end);
        Assert.True(queue.TryDequeue(out start, out end));
        Assert.Same(second, start);
        Assert.Same(third, end);
        Assert.False(queue.TryDequeue(out _, out _));
    }

    [Fact]
    public void TryEnqueue_DoesNotEnqueueSegmentWithSameNodeAtBothEnds()
    {
        var node = new Node();
        var queue = new SegmentQueue();

        bool enqueued = queue.TryEnqueue(node, node);

        Assert.False(enqueued);
        Assert.False(queue.TryDequeue(out _, out _));
    }
}
