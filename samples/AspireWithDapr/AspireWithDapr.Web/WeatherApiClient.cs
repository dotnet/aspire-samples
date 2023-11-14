using Dapr.Client;


namespace AspireWithDapr.Web;

public class WeatherApiClient(DaprClient daprClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
    
       // Get the weather from the state store if it exists
       var weather = await daprClient.GetStateAsync<WeatherForecast[]>("statestore", "weather");

        if (weather is null)
        {
            // If it doesn't exist, get it from the weather service
            weather = await daprClient.InvokeMethodAsync<WeatherForecast[]>(HttpMethod.Get, "api", "weatherforecast");
    
            await daprClient.SaveStateAsync("statestore", "weather", weather, metadata: new Dictionary<string, string>() {
                {
                    "ttlInSeconds", "120" 
                }
            });
        }

       return weather;
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
