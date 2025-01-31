namespace BlazorStateManagement.Core;
public interface IState<TState> : IState where TState : notnull
{
    public TState Value { get; }

    internal void ReplaceValue(TState valueBuilder);
}
