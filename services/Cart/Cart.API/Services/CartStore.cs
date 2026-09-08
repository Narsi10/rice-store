using System.Text.Json;
using Cart.API.Models;
using StackExchange.Redis;

namespace Cart.API.Services;

/// <summary>Reads and writes carts to Redis as JSON blobs.</summary>
public class CartStore
{
    private readonly IDatabase _redis;
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    public CartStore(IConnectionMultiplexer mux) => _redis = mux.GetDatabase();

    private static string Key(string customerId) => $"cart:{customerId}";

    public async Task<ShoppingCart> GetAsync(string customerId)
    {
        var value = await _redis.StringGetAsync(Key(customerId));
        if (value.IsNullOrEmpty)
            return new ShoppingCart { CustomerId = customerId };

        return JsonSerializer.Deserialize<ShoppingCart>((string)value!)
               ?? new ShoppingCart { CustomerId = customerId };
    }

    public async Task<ShoppingCart> SaveAsync(ShoppingCart cart)
    {
        await _redis.StringSetAsync(Key(cart.CustomerId),
            JsonSerializer.Serialize(cart), Ttl);
        return cart;
    }

    public Task DeleteAsync(string customerId) =>
        _redis.KeyDeleteAsync(Key(customerId));
}
