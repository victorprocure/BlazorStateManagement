using BlazorStateManagement.Common;

namespace BlazorStateManagement.Core;
public interface IState
{
    public string Name { get; }

    internal IStateSubscription AddChangeCallback(Action<object> changeCallback);
    internal object GetValue();
    internal void ReplaceValue(object value);
}
