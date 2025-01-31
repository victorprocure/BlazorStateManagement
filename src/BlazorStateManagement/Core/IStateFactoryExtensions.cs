namespace BlazorStateManagement.Core;
internal static class IStateFactoryExtensions
{
    public static IState<TState> CreateState<TState>(this IStateFactory factory) where TState : notnull, new()
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new State<TState>(factory);
    }
}
