
using BlazorStateManagement.Common;

namespace BlazorStateManagement.Core;
internal sealed class State : IState
{
    public State(string stateName, StateInformation stateInformation)
    {
        StateInformation = stateInformation;
    }

    public StateInformation StateInformation { get; set; }
    public string Name => StateInformation.StateName;

    IStateSubscription IState.AddChangeCallback(Action<object> changeCallback)
    {
        return StateInformation.State.AddChangeCallback(changeCallback);
    }

    object IState.GetValue()
    {
        return StateInformation.State.GetValue();
    }

    void IState.ReplaceValue(object value)
    {
        StateInformation.State.ReplaceValue(value);
    }
}
