using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using BlazorStateManagement.Common;
using BlazorStateManagement.Core;
using BlazorStateManagement.DependencyInjection;
using BlazorStateManagement.Dispatching;

using FluentScanning;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using static BlazorStateManagement.Common.StateComponentSubscriber;

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace BlazorStateManagement;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStateManagement(this IServiceCollection services)
        => AddStateManagement(services, static _ => { }, null);

    public static IServiceCollection AddStateManagement(this IServiceCollection services, Action<IStateManagementBuilder> configure)
        => AddStateManagement(services, configure, null);

    public static IServiceCollection AddStateManagement(this IServiceCollection services, IConfiguration configuration)
        => AddStateManagement(services, static _ => { }, configuration);

    public static IServiceCollection AddStateManagement(this IServiceCollection services, Action<IStateManagementBuilder> configure, IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions();

        services.TryAddSingleton<IStateFactory, StateFactory>();
        services.TryAddSingleton(typeof(IState<>), typeof(State<>));
        services.TryAddSingleton<IDispatcher, Dispatcher>();
        services.TryAddSingleton<ActionExecutor>();
        services.TryAddSingleton<StateComponentSubscriber>();

        var builder = new StateManagementBuilder(services, configuration);
        builder.AddProvider(new DefaultStateProvider());
        builder.AddActionQueuer((configuration) =>
        {
            if (!int.TryParse(configuration["QueueCapacity"], out var queueCapacity))
                queueCapacity = 100;
            return new ActionQueuer(queueCapacity);
        });

        configure(builder);
        return services;
    }

    [RequiresUnreferencedCode("This functionality cannot be trimmed")]
    public static IHost UseStateManagement(this IHost host, params Assembly[] assemblies)
    {
        var actionExecutor = host.Services.GetRequiredService<ActionExecutor>();
        var hostLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var stateComponentSubscriber = host.Services.GetRequiredService<StateComponentSubscriber>();
        ScanForStateComponents(assemblies, stateComponentSubscriber);

        _ = Task.Run(async () => await actionExecutor.StartAsync(hostLifetime.ApplicationStopping).ConfigureAwait(false));

        return host;
    }

    [RequiresUnreferencedCode("This functionality cannot be trimmed")]
    private static void ScanForStateComponents(Assembly[] assemblies, StateComponentSubscriber stateComponentSubscriber)
    {
        var scanner = new AssemblyScanner([.. assemblies.Select(a => (AssemblyProvider)a)]);

        var stateComponents = scanner.ScanForTypesThat()
            .AreAssignableTo<StateComponent>()
            .AreNotAbstractClasses()
            .AreNotInterfaces()
            .ArePublic()
            .AsTypes();

        foreach (var type in stateComponents)
        {
            if (HasStateInjected(type, out var memberProvider))
            {
                stateComponentSubscriber.AddPotentialSubscriber(new ComponentStateMemberProvider(type, memberProvider));
            }
        }
    }

    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "<Pending>")]
    private static bool HasStateInjected([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, [NotNullWhen(true)]out IEnumerable<StateMemberProvider>? stateTypes)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        var stateFields = fields.Where(IsStateField);
        var stateProperties = properties.Where(IsStateProperty);

        var fieldProviders = stateFields.Select(f => new StateMemberProvider(FieldInfo: f));
        var propertyProviders = stateProperties.Select(p => new StateMemberProvider(PropertyInfo: p));

        var providers = fieldProviders.Concat(propertyProviders).ToList();
        stateTypes = providers;
        return providers.Count > 0;
    }

    private static bool IsStateField(FieldInfo fi)
    {
        return IsStateType(fi.FieldType);
    }

    private static bool IsStateProperty(PropertyInfo property)
    {
        return IsStateType(property.PropertyType);
    }

    private static bool IsStateType(Type type)
    {
        return type.IsAssignableTo(typeof(IState));
    }


    internal readonly record struct StateMemberProvider(PropertyInfo? PropertyInfo = default, FieldInfo? FieldInfo = default);
}