using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorStateManagement.DependencyInjection;
internal sealed class StateManagementBuilder : IStateManagementBuilder
{
    public StateManagementBuilder(IServiceCollection services, IConfiguration? configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public IServiceCollection Services { get; }

    public IConfiguration? Configuration { get; }
}
