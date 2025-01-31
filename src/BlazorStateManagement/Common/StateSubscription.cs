namespace BlazorStateManagement.Common;
internal sealed class StateSubscription : IStateSubscription
{
    private readonly Action _callbackDisposer;

    public StateSubscription(string stateName, Action callbackDisposer)
    {
        _callbackDisposer = callbackDisposer;
        StateName = stateName;
    }

    public string StateName { get; }

    public void Dispose()
    {
        _callbackDisposer?.Invoke();
    }
}
