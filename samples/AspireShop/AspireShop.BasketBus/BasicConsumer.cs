using Microsoft.Extensions.Logging;
using OpenTelemetry;
using System.Diagnostics;
using System.Text;
using AspireShop.ServiceDefaults;
using RabbitMQ.Client;

namespace AspireShop.BasketBus
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// 
    /// <remarks>See https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/MicroserviceExample/Utils/Messaging/MessageReceiver.cs#L43C10-L71C1</remarks>
    public class BasicConsumer<T> : DefaultBasicConsumer where T : class
    {
        private readonly ILogger<BasicConsumer<T>> _logger;

        public BasicConsumer(IModel model, ILogger<BasicConsumer<T>> logger) : base(model)
        {
            _logger = logger;
        }

        public async Task StartAsync(IModel channel, CancellationToken stoppingToken)
        {
            var queue = typeof(T).FullName!.ToLower().Replace(".", "-");

            channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false,
                arguments: null);
            
            channel.BasicConsume(queue, autoAck: true, consumer: this);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogDebug("Consumer running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            Task.Run(async () =>
            {

                // Extract the PropagationContext of the upstream parent from the message headers.
                var parentContext =
                    SharedActivitySource.Propagator.Extract(default, properties, this.ExtractTraceContextFromBasicProperties);
                Baggage.Current = parentContext.Baggage;

                // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
                // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
                var activityName = $"{routingKey} receive";

                using var activity = SharedActivitySource.ActivitySource.StartActivity(activityName, ActivityKind.Consumer,
                    parentContext.ActivityContext);
                try
                {
                    var json = Encoding.UTF8.GetString(body.Span.ToArray());

                    _logger.LogInformation($"Message received: [{json}]");

                    var payload = System.Text.Json.JsonSerializer.Deserialize<T>(json);

                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                    AddMessagingTags(activity);
                    
                    await ProcessAsync(payload!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Message processing failed.");
                }
            });
        }

        public virtual Task ProcessAsync(T payload)
        {
            return Task.CompletedTask;
        }


        public static void AddMessagingTags(Activity? activity)
        {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // See:
            //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#messaging-attributes
            //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/rabbitmq.md
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            //activity?.SetTag("messaging.destination", DefaultExchangeName);
            //activity?.SetTag("messaging.rabbitmq.routing_key", TestQueueName);
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes!) };
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to extract trace context.");
            }

            return Enumerable.Empty<string>();
        }
    }
}
