using System.Collections.Concurrent;

namespace BlazorStateManagement.Core;
internal sealed class StateFactory : IStateFactory
{
    private readonly object _lock = new();
    private ProviderRegistration _providerRegistration;
    private readonly ConcurrentDictionary<string, State> _states = [];
    private volatile bool _disposed;

    public StateFactory(IStateProvider provider)
    {
        AddProvider(provider);
    }

    public void AddProvider(IStateProvider provider)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            AddProviderRegistration(provider, dispose: true);

            foreach (var existingState in _states)
            {
                var state = existingState.Value;
                var stateInformation = new StateInformation(provider, existingState.Key, ((IState)existingState.Value).GetValue());
                state.StateInformation = stateInformation;
            }
        }
    }

    public IState CreateState(string stateName, object initialState)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if(!_states.TryGetValue(stateName, out State? state))
        {
            lock (_lock)
            {
                if(!_states.TryGetValue(stateName, out state))
                {
                    state = new State(stateName, CreateStateInformation(stateName, initialState));
                    _states[stateName] = state;
                }
            }
        }

        return state;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                if (_providerRegistration.ShouldDispose)
                {
                    _providerRegistration.Provider.Dispose();
                }
            }
            catch
            {
                // Do nothing
            }

        }
    }

    private void AddProviderRegistration(IStateProvider provider, bool dispose)
    {
        _providerRegistration = new(provider, dispose);
    }

    private StateInformation CreateStateInformation(string stateName, object initialState)
    {
        var states = new StateInformation(_providerRegistration.Provider, stateName, initialState);
        return states;
    }
    
    private readonly record struct ProviderRegistration(IStateProvider Provider, bool ShouldDispose);
}
