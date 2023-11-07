using Grpc.Core;
using eShopLite.GrpcBasket;
using eShopLite.BasketService.Models;
using eShopLite.BasketService.Repositories;

namespace eShopLite.BasketService;

public class BasketService(IBasketRepository repository, ILogger<BasketService> logger)
    : Basket.BasketBase
{
    public override async Task<CustomerBasketResponse> GetBasketById(BasketRequest request, ServerCallContext context)
    {
        // Uncomment to force a delay for testing resiliency, etc.
        //await Task.Delay(3000);

        var data = await repository.GetBasketAsync(request.Id);

        if (data is not null)
        {
            return MapToCustomerBasketResponse(data);
        }

        return new CustomerBasketResponse();
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(CustomerBasketRequest request, ServerCallContext context)
    {
        var customerBasket = MapToCustomerBasket(request);
        var response = await repository.UpdateBasketAsync(customerBasket);

        if (response is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {request.BuyerId} does not exist"));
        }

        return MapToCustomerBasketResponse(response);
    }

    public override async Task<CheckoutCustomerBasketResponse> CheckoutBasket(CheckoutCustomerBasketRequest request, ServerCallContext context)
    {
        var buyerId = request.BuyerId;
        var basket = await repository.GetBasketAsync(buyerId);

        if (basket is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {request.BuyerId} does not exist"));
        }

        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            BuyerId = buyerId,
            Items = basket.Items,
        };

        logger.LogInformation("Checking out {Count} item(s) for BuyerId: {BuyerId}.", order.Items.Count, buyerId);

        // Checkout logic would be implemented here

        await repository.DeleteBasketAsync(buyerId);

        logger.LogInformation("Order Id {Id} submitted.", order.Id);

        return new();
    }

    public override async Task<DeleteCustomerBasketResponse> DeleteBasket(DeleteCustomerBasketRequest request, ServerCallContext context)
    {
        await repository.DeleteBasketAsync(request.BuyerId);
        return new();
    }

    private static CustomerBasketResponse MapToCustomerBasketResponse(CustomerBasket customerBasket)
    {
        var response = new CustomerBasketResponse
        {
            BuyerId = customerBasket.BuyerId
        };

        foreach (var item in customerBasket.Items)
        {
            response.Items.Add(new BasketItemResponse
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                OldUnitPrice = item.OldUnitPrice,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        return response;
    }

    private static CustomerBasket MapToCustomerBasket(CustomerBasketRequest customerBasketRequest)
    {
        var response = new CustomerBasket
        {
            BuyerId = customerBasketRequest.BuyerId
        };

        foreach (var item in customerBasketRequest.Items)
        {
            response.Items.Add(new BasketItem
            {
                Id = item.Id,
                OldUnitPrice = item.OldUnitPrice,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        return response;
    }
}
