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

        builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("https+http://apiservice"));

        HostEnvironment = builder.Environment;

        var app = builder.Build();
        Services = app.Services;
        app.Start();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(ActivatorUtilities.CreateInstance<MainForm>(app.Services));

        app.StopAsync().GetAwaiter().GetResult();
    }

    internal static IServiceProvider Services { get; private set; } = default!;

    internal static IHostEnvironment HostEnvironment { get; private set; } = default!;
}
