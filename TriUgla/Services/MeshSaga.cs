namespace TriUgla;

internal sealed class MeshSaga : IDisposable
{
    readonly Stack<Action> _compensations = [];
    bool _committed;

    public void Step(Action action, Action compensate)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(compensate);
        action();
        _compensations.Push(compensate);
    }

    public void Commit()
    {
        _committed = true;
        _compensations.Clear();
    }

    public void Dispose()
    {
        if (_committed) return;
        List<Exception>? failures = null;
        while (_compensations.TryPop(out Action? compensate))
        {
            try { compensate(); }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }
        if (failures is not null)
            throw new AggregateException("Mesh saga compensation failed.", failures);
    }
}
