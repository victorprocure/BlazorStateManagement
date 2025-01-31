namespace BlazorStateManagement.Common;
public interface IStateSubscription : IDisposable
{
    public string StateName { get; }
}
