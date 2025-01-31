using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using BlazorStateManagement.Core;

using static BlazorStateManagement.ServiceCollectionExtensions;

namespace BlazorStateManagement.Common;
internal sealed class StateComponentSubscriber
{
    private readonly ConcurrentDictionary<Type, IEnumerable<StateMemberProvider>> _stateMemberProviders = [];
    public void AddPotentialSubscriber(ComponentStateMemberProvider memberProvider)
    {
        _stateMemberProviders.TryAdd(memberProvider.ComponentType, memberProvider.MemberProviders);
    }

    public void InitializeComponentSubscribers<TStateComponent>(TStateComponent component, Action<IState> subscriberCallback)
        where TStateComponent : StateComponent
    {
        var componentType = component.GetType();
        if(!_stateMemberProviders.TryGetValue(componentType, out var memberProviders))
        {
            return;
        }

        foreach (var memberProvider in memberProviders)
        {
            switch (memberProvider)
            {
                case {FieldInfo: { } fi } when fi.GetValue(component) is IState fieldState:
                    subscriberCallback(fieldState);
                    break;
                case { PropertyInfo: { } pi } when pi.GetValue(component) is IState propertyState:
                    subscriberCallback(propertyState);
                    break;
            }
        }
    }

    internal readonly record struct ComponentStateMemberProvider(Type ComponentType, IEnumerable<StateMemberProvider> MemberProviders);
}
