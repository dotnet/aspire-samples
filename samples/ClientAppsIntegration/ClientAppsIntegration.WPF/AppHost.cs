﻿namespace ClientAppsIntegration.WPF;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddAppDefaults();

        builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("https+http://apiservice"));
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<MainWindow>();

        var appHost = builder.Build();
        var app = appHost.Services.GetRequiredService<App>();
        var mainWindow = appHost.Services.GetRequiredService<MainWindow>();

        appHost.Start();
        app.Run(mainWindow);

        appHost.StopAsync().GetAwaiter().GetResult();
    }
}
