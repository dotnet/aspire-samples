using System.Threading.Channels;
using AspireShop.BasketBus;
using AspireShop.BasketService.Models;
using AspireShop.Chaos;
using RabbitMQ.Client;

namespace AspireShop.BasketWorker;

public class CustomerBasketConsumer : BasicConsumer<CustomerBasket>
{
    private readonly ChaosProvider _chaosProvider;

    public CustomerBasketConsumer(IModel model, ILogger<CustomerBasketConsumer> logger, ChaosProvider chaosProvider) : base(model, logger)
    {
        _chaosProvider = chaosProvider;
    }

    public override async Task ProcessAsync(CustomerBasket payload)
    {
        await _chaosProvider.PonderChaosAsync();
            
        // Simulate some work
        await Task.Delay(1000);
    }


}
