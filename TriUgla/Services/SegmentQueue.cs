namespace TriUgla;

public sealed class SegmentQueue(int capacity = 8)
{
    readonly Queue<(Node Start, Node End)> _segments = new(capacity);

    public bool TryDequeue(out Node start, out Node end)
    {
        if (_segments.TryDequeue(out var segment))
        {
            start = segment.Start;
            end = segment.End;
            return true;
        }

        start = null!;
        end = null!;
        return false;
    }

    public void Enqueue(Node start, Node end)
    {
        if (!ReferenceEquals(start, end))
        {
            _segments.Enqueue((start, end));
        }
    }
}
