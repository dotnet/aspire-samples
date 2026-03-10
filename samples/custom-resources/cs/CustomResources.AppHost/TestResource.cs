// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

static class TestResourceExtensions
{
    internal static readonly string[] callback = ["Starting", "Running", "Finished", "Uploading", "Downloading", "Processing", "Provisioning"];
    internal static readonly string[] callbackArray = ["info", "success", "warning", "error"];

    public static IResourceBuilder<TestResource> AddTestResource(this IDistributedApplicationBuilder builder, string name)
    {
        var rb = builder.AddResource(new TestResource(name))
            .WithInitialState(new()
            {
                ResourceType = "Test Resource",
                State = "Starting",
                Properties = [
                    new("P1", "P2"),
                    new(CustomResourceKnownProperties.Source, "Custom")
                ]
            })
            .ExcludeFromManifest();

        rb.OnInitializeResource((resource, e, ct) =>
        {
            var states = callback;
            var stateStyles = callbackArray;

            var loggerService = e.Services.GetRequiredService<ResourceLoggerService>();
            var notificationService = e.Services.GetRequiredService<ResourceNotificationService>();
            var appLifetime = e.Services.GetRequiredService<IHostApplicationLifetime>();
            var logger = loggerService.GetLogger(resource);

            Task.Run(async () =>
            {
                var seconds = Random.Shared.Next(2, 12);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Starting test resource {ResourceName} with update interval {Interval} seconds", resource.Name, seconds);
                }

                await notificationService.PublishUpdateAsync(resource, state => state with
                {
                    Properties = [.. state.Properties, new("Interval", seconds.ToString(CultureInfo.InvariantCulture))]
                });

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

                while (await timer.WaitForNextTickAsync(appLifetime.ApplicationStopping))
                {
                    var randomState = states[Random.Shared.Next(0, states.Length)];
                    var randomStyle = stateStyles[Random.Shared.Next(0, stateStyles.Length)];
                    await notificationService.PublishUpdateAsync(resource, state => state with
                    {
                        State = new(randomState, randomStyle)
                    });

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Test resource {ResourceName} is now in state {State}", resource.Name, randomState);
                    }
                }
            },
            ct);

            return Task.CompletedTask;
        });

        return rb;
    }
}

sealed class TestResource(string name) : Resource(name)
{

}
