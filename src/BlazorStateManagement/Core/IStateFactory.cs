namespace BlazorStateManagement.Core;
public interface IStateFactory : IDisposable
{
    IState CreateState(string stateName, object initialState);
}
