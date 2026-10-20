using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

public class ProcessRabbitMQMessage : BackgroundService{
    private readonly ILogger<ProcessRabbitMQMessage> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _messageConnection;
    private IModel? _messageChannel;
    private EventingBasicConsumer consumer;

    public ProcessRabbitMQMessage(ILogger<ProcessRabbitMQMessage> logger, IServiceProvider serviceProvider, IConnection? messageConnection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string queueName = "testMessage";
        _messageConnection = _serviceProvider.GetRequiredService<IConnection>();

        _messageChannel = _messageConnection.CreateModel();
        _messageChannel.QueueDeclare(queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        consumer = new EventingBasicConsumer(_messageChannel);
        consumer.Received += ProcessMessageAsync;

        _messageChannel.BasicConsume(queue: queueName,
            autoAck: true,
            consumer: consumer);
        return Task.CompletedTask;
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        consumer.Received -= ProcessMessageAsync;
        _messageChannel?.Dispose();
    }
    private void ProcessMessageAsync(object? sender, BasicDeliverEventArgs args)
    {
        string message = Encoding.UTF8.GetString(args.Body.ToArray());
        _logger.LogInformation("Message retrieved from queue at {now}. Message Text: {text}", DateTime.Now, message);
       // var message = args.Body;
    }
}