using BlazorStateManagement.Core;
using BlazorStateManagement.Dispatching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorStateManagement.DependencyInjection;
internal static class StateManagementBuilderExtensions
{
    public static IStateManagementBuilder AddProvider(this IStateManagementBuilder builder, IStateProvider provider)
    {
        builder.ClearProviders();
        builder.Services.TryAddSingleton(provider);

        return builder;
    }

    public static IStateManagementBuilder AddActionQueuer(this IStateManagementBuilder builder, IActionQueuer actionQueuer)
    {
        builder.ClearActionQueuer();
        builder.Services.TryAddSingleton(actionQueuer);

        return builder;
    }

    public static IStateManagementBuilder AddActionQueuer(this IStateManagementBuilder builder, Func<IActionQueuer> factory)
    {
        builder.AddActionQueuer(factory());

        return builder;
    }

    public static IStateManagementBuilder AddActionQueuer(this IStateManagementBuilder builder, Func<IConfiguration, IActionQueuer> factory)
    {
        if(builder.Configuration is null)
        {
            throw new InvalidOperationException("Builder configuration is not set. Call `AddStateManagement` with `IConfiguration` overload");
        }

        builder.AddActionQueuer(factory(builder.Configuration));

        return builder;
    }

    private static IStateManagementBuilder ClearActionQueuer(this IStateManagementBuilder builder)
    {
        builder.Services.RemoveAll<IActionQueuer>();
        return builder;
    }
    private static IStateManagementBuilder ClearProviders(this IStateManagementBuilder builder)
    {
        builder.Services.RemoveAll<IStateProvider>();
        return builder;
    }
}
