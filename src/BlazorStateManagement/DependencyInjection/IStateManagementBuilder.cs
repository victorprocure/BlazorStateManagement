using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorStateManagement.DependencyInjection;
public interface IStateManagementBuilder
{
    IServiceCollection Services { get; }

    IConfiguration? Configuration { get; }
}
