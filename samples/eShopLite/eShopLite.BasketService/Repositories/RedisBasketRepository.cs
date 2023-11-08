using System.Text.Json;
using StackExchange.Redis;
using eShopLite.BasketService.Models;

namespace eShopLite.BasketService.Repositories;

public class RedisBasketRepository(ILogger<RedisBasketRepository> logger, IConnectionMultiplexer redis) : IBasketRepository
{
    private readonly ILogger<RedisBasketRepository> _logger = logger;
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly IDatabase _database = redis.GetDatabase();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<bool> DeleteBasketAsync(string id)
    {
        return await _database.KeyDeleteAsync(id);
    }

    public IEnumerable<string> GetUsers()
    {
        var server = GetServer();
        var data = server.Keys();

        return data?.Select(k => k.ToString()) ?? Enumerable.Empty<string>();
    }

    public async Task<CustomerBasket?> GetBasketAsync(string customerId)
    {
        var data = await _database.StringGetAsync(customerId);

        if (data.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<CustomerBasket>(data!, _jsonSerializerOptions);
    }

    public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket)
    {
        if (basket.BuyerId == null)
        {
            return null;
        }

        var created = await _database.StringSetAsync(basket.BuyerId, JsonSerializer.Serialize(basket, _jsonSerializerOptions));

        if (!created)
        {
            _logger.LogInformation("Problem occur persisting the item.");
            return null;
        }

        _logger.LogInformation("Basket item persisted successfully.");

        return await GetBasketAsync(basket.BuyerId);
    }

    private IServer GetServer()
    {
        var endpoint = _redis.GetEndPoints();
        return _redis.GetServer(endpoint.First());
    }
}
