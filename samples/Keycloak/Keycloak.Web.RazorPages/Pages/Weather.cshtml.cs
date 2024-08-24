using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Keycloak.Web.RazorPages.Pages
{
    public class WeatherModel(WeatherApiClient weatherApi) : PageModel
    {
        public WeatherForecast[] Forecasts { get; set; } = [];

        public async Task OnGet()
        {
            Forecasts = await weatherApi.GetWeatherAsync();
        }
    }
}
