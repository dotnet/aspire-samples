using MassTransit;

namespace AspireWithMasstransit.ApiService;

public class HelloAspireEventConsumer(ILogger<HelloAspireEventConsumer> logger) : IConsumer<HelloAspireEvent>
{
    public Task Consume(ConsumeContext<HelloAspireEvent> context)
    {
        logger.LogInformation("Received: {Message}", context.Message.Message);
        return Task.CompletedTask;
    }
}

public record HelloAspireEvent(string Message);
