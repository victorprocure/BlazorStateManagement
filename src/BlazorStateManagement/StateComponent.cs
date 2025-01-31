using System.Collections.Concurrent;

using BlazorStateManagement.Common;
using BlazorStateManagement.Core;

using Microsoft.AspNetCore.Components;

namespace BlazorStateManagement;
public abstract class StateComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    private StateComponentSubscriber StateComponentSubscriber { get; set; } = default!;

    private readonly ConcurrentDictionary<Type, Func<object, ValueTask>> _stateCallbacks = [];
    private readonly List<IStateSubscription> _stateSubscriptions = [];
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach(var stateSubscription in _stateSubscriptions)
        {
            stateSubscription.Dispose();
        }

        _stateSubscriptions.Clear();

        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        StateComponentSubscriber.InitializeComponentSubscribers(this, SubscribeToStateChange);
    }

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        return ValueTask.CompletedTask;
    }

    protected void RegisterStateChangeCallback<TState, TStateValue>(Action<TStateValue> callback)
            where TState: IState<TStateValue> where TStateValue : notnull, new()
    {
        RegisterStateChangeCallback<TState, TStateValue>(s => { callback(s); return ValueTask.CompletedTask; });
    }

    protected void RegisterStateChangeCallback<TState, TStateValue>(Func<TStateValue, ValueTask> callback)
        where TState : IState<TStateValue> where TStateValue : notnull, new()
    {
        _stateCallbacks.TryAdd(typeof(TState), s => callback((TStateValue)s));
    }

    private void StateChanged(object stateValue, IState state)
    {
        if (_stateCallbacks.TryGetValue(state.GetType(), out var callback))
        {
            Task.Run(async () => await callback(stateValue).ConfigureAwait(false));
        }

        InvokeAsync(StateHasChanged);
    }

    private void SubscribeToStateChange(IState state)
    {
        if(_stateSubscriptions.Exists(s => s.StateName == state.Name))
        {
            return;
        }

        var subscription = state.AddChangeCallback(o => StateChanged(o, state));
        _stateSubscriptions.Add(subscription);
    }
}
