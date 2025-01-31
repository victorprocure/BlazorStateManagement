using System.Diagnostics.CodeAnalysis;

namespace BlazorStateManagement.Dispatching;
internal sealed class ActionExecutor : IDisposable
{
    private CancellationTokenSource? _stoppingToken;
    private Task? _executeTask;
    private readonly IActionQueuer _actionQueuer;

    public ActionExecutor(IActionQueuer actionQueuer)
    {
        _actionQueuer = actionQueuer;
    }

    public void Dispose() => _stoppingToken?.Dispose();

    [MemberNotNull(nameof(_executeTask), nameof(_stoppingToken))]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executeTask = ExecuteAsync(_stoppingToken.Token);

        if (_executeTask.IsCompleted)
        {
            return _executeTask;
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var workItem = await _actionQueuer.DequeueAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await workItem(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Do nothing
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if(_executeTask == null)
        {
            return;
        }

        try
        {
            await _stoppingToken!.CancelAsync().ConfigureAwait(false);
        }
        finally
        {
            await _executeTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
