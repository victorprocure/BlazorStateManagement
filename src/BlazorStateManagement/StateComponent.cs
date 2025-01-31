using System.Collections.Concurrent;

using BlazorStateManagement.Common;
using BlazorStateManagement.Core;

using Microsoft.AspNetCore.Components;

namespace BlazorStateManagement;
public abstract class StateComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    private StateComponentSubscriber StateComponentSubscriber { get; set; } = default!;

    [Inject]
    private IStateFactory StateFactory { get; set; } = default!;

    private readonly ConcurrentDictionary<string, Func<object, ValueTask>> _stateCallbacks = [];
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

    protected void RegisterStateChangeCallback<TStateValue>(Action<TStateValue> callback)
            where TStateValue : notnull, new()
    {
        RegisterStateChangeCallback<TStateValue>(s => { callback(s); return ValueTask.CompletedTask; });
    }

    protected void RegisterStateChangeCallback<TStateValue>(Func<TStateValue, ValueTask> callback)
        where TStateValue : notnull, new()
    {
        var state = StateFactory.CreateState<TStateValue>();
        _stateCallbacks.TryAdd(state.Name, s => callback((TStateValue)s));
    }

    private void StateChanged(object stateValue, IState state)
    {
        if (_stateCallbacks.TryGetValue(state.Name, out var callback))
        {
            InvokeAsync(async () => await callback(stateValue).ConfigureAwait(false));
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
