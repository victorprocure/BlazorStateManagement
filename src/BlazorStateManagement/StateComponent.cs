using System.Collections.Concurrent;

using BlazorStateManagement.Common;
using BlazorStateManagement.Core;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorStateManagement;

/// <summary>
/// Mandatory base class for components that wish to react to events from <see cref="IState"/> and <see cref="IState{TState}"/> change events.
/// </summary>
/// <remarks>
/// This is a complete re-implementation of <see cref="ComponentBase"/> however it removes the need for the inheritor to call <see cref="OnInitialized"/> on the base class
/// </remarks>
public abstract class StateComponent : IComponent, IHandleEvent, IHandleAfterRender, IAsyncDisposable
{
    private readonly RenderFragment _renderFragment;
    private readonly ConcurrentDictionary<string, Func<object, ValueTask>> _stateCallbacks = [];
    private readonly List<IStateSubscription> _stateSubscriptions = [];
    private bool _disposed;
    private bool _hasCalledOnAfterRender;
    private bool _hasNeverRendered = true;
    private bool _hasPendingQueuedRender;
    private bool _initialized;
    private RenderHandle _renderHandle;

    /// <summary>
    /// Constructs an instance of <see cref="StateComponent"/>
    /// </summary>
    protected StateComponent()
    {
        _renderFragment = builder =>
        {
            _hasPendingQueuedRender = false;
            _hasNeverRendered = false;
            BuildRenderTree(builder);
        };
    }

    [Inject]
    private StateComponentSubscriber StateComponentSubscriber { get; set; } = default!;

    [Inject]
    private IStateFactory StateFactory { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        if (_renderHandle.IsInitialized)
            throw new InvalidOperationException($"The render handle is already set. Cannot initialize a {nameof(StateComponent)} more than once.");

        _renderHandle = renderHandle;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var stateSubscription in _stateSubscriptions)
        {
            stateSubscription.Dispose();
        }

        _stateSubscriptions.Clear();

        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem item, object? arg)
    {
        var task = item.InvokeAsync(arg);
        var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion
            && task.Status != TaskStatus.Canceled;

        StateHasChanged();
        return shouldAwaitTask
            ? CallStateHasChangedOnAsyncCompletion(task)
            : Task.CompletedTask;
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        var firstRender = !_hasCalledOnAfterRender;
        _hasCalledOnAfterRender = true;

        OnAfterRender(firstRender);

        return OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc cref="ComponentBase.SetParametersAsync(ParameterView)" />
    public virtual Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (!_initialized)
        {
            _initialized = true;

            return RunInitAndSetParametersAsync();
        }

        return CallOnParametersSetAsync();
    }

    /// <inheritdoc cref="ComponentBase.BuildRenderTree(RenderTreeBuilder)" />
    protected virtual void BuildRenderTree(RenderTreeBuilder builder)
    {

    }

    /// <inheritdoc cref="ComponentBase.DispatchExceptionAsync(Exception)" />
    protected Task DispatchExceptionAsync(Exception exception)
        => _renderHandle.DispatchExceptionAsync(exception);

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="ComponentBase.InvokeAsync(Action)" />
    protected Task InvokeAsync(Action workItem)
        => _renderHandle.Dispatcher.InvokeAsync(workItem);

    /// <inheritdoc cref="ComponentBase.InvokeAsync(Func{Task})" />
    protected Task InvokeAsync(Func<Task> workItem)
        => _renderHandle.Dispatcher.InvokeAsync(workItem);

    /// <inheritdoc cref="ComponentBase.OnAfterRender(bool)" />
    protected virtual void OnAfterRender(bool firstRender)
    {
    }

    /// <inheritdoc cref="ComponentBase.OnAfterRenderAsync(bool)" />
    protected virtual Task OnAfterRenderAsync(bool firstRender)
        => Task.CompletedTask;

    /// <inheritdoc cref="ComponentBase.OnInitialized" />
    protected virtual void OnInitialized()
    {

    }

    /// <inheritdoc cref="ComponentBase.OnInitializedAsync" />
    protected virtual Task OnInitializedAsync()
        => Task.CompletedTask;

    /// <inheritdoc cref="ComponentBase.OnParametersSet" />
    protected virtual void OnParametersSet()
    {

    }

    /// <inheritdoc cref="ComponentBase.OnParametersSetAsync" />
    protected virtual Task OnParametersSetAsync()
        => Task.CompletedTask;

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

    /// <inheritdoc cref="ComponentBase.ShouldRender" />
    protected virtual bool ShouldRender()
        => true;

    /// <inheritdoc cref="ComponentBase.StateHasChanged" />
    protected void StateHasChanged()
    {
        if (_hasPendingQueuedRender)
            return;


        if (_hasNeverRendered || ShouldRender() || _renderHandle.IsRenderingOnMetadataUpdate)
        {
            _hasPendingQueuedRender = true;

            try
            {
                _renderHandle.Render(_renderFragment);
            }
            catch
            {
                _hasPendingQueuedRender = false;
                throw;
            }
        }
    }

    private Task CallOnParametersSetAsync()
    {
        OnParametersSet();
        var task = OnParametersSetAsync();

        var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion
            && task.Status != TaskStatus.Canceled;

        StateHasChanged();

        return shouldAwaitTask
            ? CallStateHasChangedOnAsyncCompletion(task)
            : Task.CompletedTask;
    }

    private async Task CallStateHasChangedOnAsyncCompletion(Task task)
    {
        try
        {
            await task.ConfigureAwait(true);
        }
        catch
        {
            if (task.IsCanceled)
            {
                return;
            }

            throw;
        }

        StateHasChanged();
    }

    private void OnInitializedInternal()
    {
        StateComponentSubscriber.InitializeComponentSubscribers(this, SubscribeToStateChange);

        OnInitialized();
    }
    private async Task RunInitAndSetParametersAsync()
    {
        OnInitializedInternal();

        var task = OnInitializedAsync();
        if(task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled)
        {
            StateHasChanged();

            try
            {
                await task.ConfigureAwait(true);
            }
            catch
            {
                if (!task.IsCanceled)
                    throw;
            }
        }

        await CallOnParametersSetAsync().ConfigureAwait(false);
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
        if (_stateSubscriptions.Exists(s => s.StateName == state.Name))
        {
            return;
        }

        var subscription = state.AddChangeCallback(o => StateChanged(o, state));
        _stateSubscriptions.Add(subscription);
    }
}