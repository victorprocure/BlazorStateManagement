using System.Diagnostics;

using BlazorStateManagement.Common;

namespace BlazorStateManagement.Core;
[DebuggerDisplay($"{{{nameof(DebuggerToString)}(),nq}}")]
internal sealed class State<T> : IState<T> where T : notnull, new()
{
    private readonly IState _state;

    public T Value => (T)_state.GetValue();
    public string Name => _state.Name;

    public State(IStateFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _state = factory.CreateState(GetStateName(), new T());
    }

    private static string GetStateName()
    {
        return TypeNameHelper.GetTypeDisplayName<T>(includeGenericParameters: false, nestedTypeDelimiter: '.');
    }

    private string DebuggerToString()
    {
        return DebuggerDisplayFormatting.DebuggerToString(GetStateName(), this);
    }

    object IState.GetValue()
    {
        return _state.GetValue();
    }

    void IState.ReplaceValue(object value)
    {
        _state.ReplaceValue(value);
    }

    IStateSubscription IState.AddChangeCallback(Action<object> changeCallback)
    {
        return _state.AddChangeCallback(changeCallback);
    }

    void IState<T>.ReplaceValue(T valueBuilder)
    {
        _state.ReplaceValue(valueBuilder);
    }
}
