namespace BlazorStateManagement.Dispatching;
public interface IActionQueuer
{
    ValueTask QueueActionWorkAsync(Func<CancellationToken, ValueTask> action);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken = default);
}
