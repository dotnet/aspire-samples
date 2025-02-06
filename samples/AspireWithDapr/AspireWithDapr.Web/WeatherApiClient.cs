using Dapr.Client;


namespace AspireWithDapr.Web;


public class WeatherApiClient(DaprClient daprClient)
{
    const string stateStore = "statestore";
    const string stateTTL = "120";
    const string apiAppId = "api";

    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
    
       // Get the weather from the state store if it exists
       var weatherData = await daprClient.GetStateAsync<WeatherForecast[]>(stateStore, "weather");

        if (weatherData is null)
        {
            // If it doesn't exist, get it from the weather service
            weatherData = await daprClient.InvokeMethodAsync<WeatherForecast[]>(HttpMethod.Get, apiAppId, "weatherforecast");
    
            await daprClient.SaveStateAsync(stateStore, "weather", weatherData, metadata: new Dictionary<string, string>() {
                {
                    "ttlInSeconds", stateTTL 
                }
            });
        }

       return weatherData;
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
