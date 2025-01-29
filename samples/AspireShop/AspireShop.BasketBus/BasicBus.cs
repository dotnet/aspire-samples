using System.Diagnostics;
using System.Text;
using AspireShop.ServiceDefaults;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace AspireShop.BasketBus;

/// <summary>
/// 
/// </summary>
/// <remarks>https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/MicroserviceExample/Utils/Messaging/MessageSender.cs</remarks>
public class BasicBus : IBus
{
    private readonly IConnection _connection;
    private readonly ILogger<BasicBus> _logger;

    public BasicBus(IConnection connection, ILogger<BasicBus> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task PublishAsync<T>(T payload)
    {

        using var channel = _connection.CreateModel();
        var queue = typeof(T).FullName!.ToLower().Replace(".", "-");
        try
        {
            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
            var activityName = $"{queue} send";

            using var activity = SharedActivitySource.ActivitySource.StartActivity(activityName, ActivityKind.Producer);
            var props = channel.CreateBasicProperties();

            // Depending on Sampling (and whether a listener is registered or not), the
            // activity above may not be created.
            // If it is created, then propagate its context.
            // If it is not created, the propagate the Current context,
            // if any.
            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
            SharedActivitySource.Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props, this.InjectTraceContextIntoBasicProperties);

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: string.Empty, queue, body: body, basicProperties: props);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message publishing failed.");
            throw;
        }
    }


    private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
    {
        try
        {
            if (props.Headers == null)
            {
                props.Headers = new Dictionary<string, object>();
            }

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}
