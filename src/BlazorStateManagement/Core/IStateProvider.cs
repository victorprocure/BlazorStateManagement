namespace BlazorStateManagement.Core;
public interface IStateProvider : IDisposable
{
    IState CreateState(string stateName, object initialValue);
}
