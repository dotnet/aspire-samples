using RabbitMQ.Client;
using AspireShop.Chaos;

namespace AspireShop.BasketWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConnection _connection;
        private readonly ChaosProvider _chaosProvider;

        public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, IConnection connection, ChaosProvider chaosProvider)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _connection = connection;
            _chaosProvider = chaosProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            using var channel = _connection.CreateModel();

            var consumer = new CustomerBasketConsumer(channel, _loggerFactory.CreateLogger<CustomerBasketConsumer>(), _chaosProvider);

            await consumer.StartAsync(channel, stoppingToken);
        }
    }
}
