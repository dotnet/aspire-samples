namespace ApiService.Tests;

public class ApiServiceTests : IClassFixture<DistributedApplicationFixture<Program>>
{
    private readonly HttpClient _httpClient;

    public ApiServiceTests(DistributedApplicationFixture<Program> appHostFixture)
    {
        _httpClient = appHostFixture.CreateClient("apiservice");
    }

    [Fact]
    public async void WeatherForecast_Returns_200()
    {
        var forecasts = await _httpClient.GetFromJsonAsync<WeatherForecast[]>("weatherforecast");

        Assert.NotNull(forecasts);
        Assert.NotEmpty(forecasts);
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
