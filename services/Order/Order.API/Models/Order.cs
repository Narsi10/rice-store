using BuildingBlocks.Domain;

namespace Order.API.Models;

public enum OrderStatus
{
    Pending = 0,        // created, waiting on stock + payment
    StockReserved = 1,
    Paid = 2,
    Confirmed = 3,      // saga succeeded; awaiting admin review
    Cancelled = 4,      // saga failed at some step
    Approved = 5,       // admin approved the order for fulfillment
    Rejected = 6        // admin rejected the order
}

public class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class Order : Entity
{
    public string CustomerId { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();

    // Admin review fields.
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }
    public string? ReviewNote { get; set; }

    // Payment fields.
    public string? PaymentMethod { get; set; }
    public string? PaymentInstrument { get; set; }
    public string? TransactionId { get; set; }
}
