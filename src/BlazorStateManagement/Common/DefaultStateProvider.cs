using BlazorStateManagement.Core;

namespace BlazorStateManagement.Common;
internal sealed class DefaultStateProvider : IStateProvider
{
    public IState CreateState(string stateName, object initialValue)
    {
        return new DefaultState(stateName, initialValue);
    }

    public void Dispose()
    {
        // Do nothing
    }

    internal sealed record DefaultState : IState
    {
        private object _currentValue;
        private Action<object>? _stateSubscribers;

        public DefaultState(string stateName, object initialValue)
        {
            Name = stateName;
            _currentValue = initialValue;
        }

        public string Name { get; }

        IStateSubscription IState.AddChangeCallback(Action<object> changeCallback)
        {
            _stateSubscribers += changeCallback;
            return new StateSubscription(Name, () => _stateSubscribers -= changeCallback);
        }

        object IState.GetValue()
        {
            return _currentValue;
        }

        void IState.ReplaceValue(object value)
        {
            _currentValue = value;
            _stateSubscribers?.Invoke(value);
        }
    }
}
