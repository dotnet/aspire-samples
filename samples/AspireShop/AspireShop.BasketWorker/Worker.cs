using System.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace AspireShop.BasketWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnection _connection;

        public Worker(ILogger<Worker> logger, IConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            using var channel = _connection.CreateModel();

            var queue = "interest";
            
            channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false,
                arguments: null);
            

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Received {message}");
            };

            channel.BasicConsume(queue, autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
