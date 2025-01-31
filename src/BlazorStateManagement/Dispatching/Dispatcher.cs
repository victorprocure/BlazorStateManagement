using BlazorStateManagement.Core;

namespace BlazorStateManagement.Dispatching;
internal sealed class Dispatcher : IDispatcher
{
    private readonly IActionQueuer _actionQueuer;
    private readonly IStateFactory _stateFactory;

    public Dispatcher(IActionQueuer actionQueuer, IStateFactory stateFactory)
    {
        _actionQueuer = actionQueuer;
        _stateFactory = stateFactory;
    }

    public void Dispatch<TState>(Func<TState, TState> action)
        where TState : notnull, new()
    {
        Task.Run(async () => await DispatchAsync<TState>(ProcessStateAction).ConfigureAwait(false));

        Task<TState> ProcessStateAction(TState state)
        {
            return Task.Run(() => action(state));
        }
    }

    public async Task DispatchAsync<TState>(Func<TState, Task<TState>> action, CancellationToken cancellationToken = default)
        where TState : notnull, new()
    {
        var state = _stateFactory.CreateState<TState>();
        await _actionQueuer.QueueActionWorkAsync(ProcessStateAction).ConfigureAwait(false);


        async ValueTask ProcessStateAction(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;

            var newValue = await action(state.Value).ConfigureAwait(false);
            state.ReplaceValue(newValue);
        }
    }
}
