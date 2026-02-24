using MassTransit;

namespace AspireWithMassTransit.ApiService;

public class HelloAspireEventConsumer : IConsumer<HelloAspireEvent>
{
    private readonly ILogger<HelloAspireEventConsumer> _logger;

    public HelloAspireEventConsumer(ILogger<HelloAspireEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<HelloAspireEvent> context)
    {
        _logger.LogInformation("Received: {Message}", context.Message.Message);
        return Task.CompletedTask;
    }
}

public record HelloAspireEvent(string Message);
