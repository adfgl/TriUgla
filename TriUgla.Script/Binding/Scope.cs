namespace TriUgla.Script;

public sealed class Scope
{
    readonly Stack<Frame> _frames = new();
    int _nextFrameId;

    public Scope()
    {
        _frames.Push(new Frame(_nextFrameId++));
    }

    public int Depth => _frames.Count;

    public IReadOnlyCollection<string> DeclaredVariables => _frames.Peek().Variables;

    public IDisposable Open()
    {
        var frame = new Frame(_nextFrameId++);
        _frames.Push(frame);
        return new ScopeLease(this, frame.Id);
    }

    public bool Declare(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _frames.Peek().Variables.Add(name);
    }

    public bool IsDeclared(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _frames.Any(frame => frame.Variables.Contains(name));
    }

    public bool IsDeclaredInCurrentScope(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _frames.Peek().Variables.Contains(name);
    }

    void Close(int frameId)
    {
        if (_frames.Count == 1)
        {
            throw new InvalidOperationException("The root scope cannot be closed.");
        }

        if (_frames.Peek().Id != frameId)
        {
            throw new InvalidOperationException("Scopes must be closed in reverse order.");
        }

        _frames.Pop();
    }

    sealed class Frame(int id)
    {
        public int Id { get; } = id;
        public HashSet<string> Variables { get; } = new(StringComparer.Ordinal);
    }

    sealed class ScopeLease(Scope owner, int frameId) : IDisposable
    {
        Scope? _owner = owner;

        public void Dispose()
        {
            Scope? currentOwner = Interlocked.Exchange(ref _owner, null);
            currentOwner?.Close(frameId);
        }
    }
}
