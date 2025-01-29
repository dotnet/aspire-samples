namespace AspireShop.BasketBus;

public interface IBus
{
    Task PublishAsync<T>(T payload);
}
