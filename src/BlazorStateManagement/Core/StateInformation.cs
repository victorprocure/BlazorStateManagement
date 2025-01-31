namespace BlazorStateManagement.Core;
internal readonly record struct StateInformation
{
    public StateInformation(IStateProvider provider, string stateName, object initialState)
    {
        ProviderType = provider.GetType();
        State = provider.CreateState(stateName, initialState);
        StateName = stateName;
        InitialState = initialState;
    }

    public Type ProviderType { get; }
    public IState State { get; }
    public string StateName { get; }
    public object InitialState { get; }
}
