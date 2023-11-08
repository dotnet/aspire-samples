using Dapr.Client;

namespace AspireWithDapr.Web;

public class WeatherApiClient(DaprClient daprClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
        return await daprClient.InvokeMethodAsync<WeatherForecast[]>(HttpMethod.Get, "api", "weatherforecast");
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
