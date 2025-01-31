using BlazorStateManagement.Demo.Data;

namespace BlazorStateManagement.Demo.FetchData;

internal sealed record FetchState
{
    public WeatherForecast[]? CurrentForcasts { get; init; }
}
