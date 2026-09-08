namespace BuildingBlocks.IntegrationEvents;

/// <summary>An order line as it travels between services.</summary>
public record OrderLine(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Published by the Order service when a customer places an order.
/// Inventory subscribes to reserve stock.
/// </summary>
public record OrderPlaced : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = default!;
    public string CustomerEmail { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<OrderLine> Lines { get; init; } = Array.Empty<OrderLine>();
}

/// <summary>Published by Inventory when all lines are successfully reserved.</summary>
public record StockReserved : IntegrationEvent
{
    public Guid OrderId { get; init; }
}

/// <summary>Published by Inventory when stock cannot be reserved (compensating path).</summary>
public record StockRejected : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = default!;
}

/// <summary>Published by Payment after a successful charge.</summary>
public record PaymentCompleted : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string TransactionId { get; init; } = default!;
}

/// <summary>Published by Payment when the charge fails (compensating path).</summary>
public record PaymentFailed : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = default!;
}

/// <summary>Published by Order once stock + payment both succeed.</summary>
public record OrderConfirmed : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = default!;
}

/// <summary>Published by Order when the saga fails and the order is cancelled.</summary>
public record OrderCancelled : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = default!;
    public string Reason { get; init; } = default!;
}

/// <summary>Published by Order when an admin approves an order.</summary>
public record OrderApproved : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = default!;
    public string CustomerEmail { get; init; } = default!;
}

/// <summary>Published by Order when an admin rejects an order.</summary>
public record OrderRejected : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = default!;
    public string CustomerEmail { get; init; } = default!;
    public string Reason { get; init; } = default!;
}
