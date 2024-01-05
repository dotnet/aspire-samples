namespace ClientAppsIntegration.WinForms;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddAppDefaults();

        var scheme = builder.Environment.IsDevelopment() ? "http" : "https";
        builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new($"{scheme}://apiservice"));

        var app = builder.Build();
        Services = app.Services;
        app.Start();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(ActivatorUtilities.CreateInstance<MainForm>(app.Services));

        app.StopAsync().GetAwaiter().GetResult();
    }

    public static IServiceProvider Services { get; private set; } = default!;
}
