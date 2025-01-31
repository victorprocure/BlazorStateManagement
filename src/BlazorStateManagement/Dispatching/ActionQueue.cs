using System.Threading.Channels;

namespace BlazorStateManagement.Dispatching;
internal sealed class ActionQueuer : IActionQueuer
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    public ActionQueuer(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return workItem;
    }

    public async ValueTask QueueActionWorkAsync(Func<CancellationToken, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _queue.Writer.WriteAsync(action).ConfigureAwait(false);
    }
}
