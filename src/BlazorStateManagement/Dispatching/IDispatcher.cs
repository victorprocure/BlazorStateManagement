namespace BlazorStateManagement.Dispatching;
public interface IDispatcher
{
    public void Dispatch<TState>(Func<TState, TState> action)
        where TState : notnull, new();

    public Task DispatchAsync<TState>(Func<TState, Task<TState>> action, CancellationToken cancellationToken = default)
        where TState : notnull, new();
}
