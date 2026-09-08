namespace Cart.API.Models;

/// <summary>A single line in a customer's cart.</summary>
public class CartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// A customer's shopping cart. Keyed by customer id and stored as JSON in Redis
/// for fast reads/writes. Not persisted to MSSQL because carts are transient.
/// </summary>
public class ShoppingCart
{
    public string CustomerId { get; set; } = default!;
    public List<CartItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.LineTotal);
}
