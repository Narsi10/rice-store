using BuildingBlocks.IntegrationEvents;
using MassTransit;
using Order.API.Data;
using Order.API.Models;

namespace Order.API.Consumers;

/// <summary>Stock reserved -> mark order, then wait for payment.</summary>
public class StockReservedConsumer : IConsumer<StockReserved>
{
    private readonly OrderDbContext _db;
    public StockReservedConsumer(OrderDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<StockReserved> ctx)
    {
        var order = await _db.Orders.FindAsync(ctx.Message.OrderId);
        if (order is null) return;

        if (order.Status == OrderStatus.Pending)
            order.Status = OrderStatus.StockReserved;

        await _db.SaveChangesAsync();
    }
}

/// <summary>Stock rejected -> cancel the order (compensation).</summary>
public class StockRejectedConsumer : IConsumer<StockRejected>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publish;

    public StockRejectedConsumer(OrderDbContext db, IPublishEndpoint publish)
    {
        _db = db;
        _publish = publish;
    }

    public async Task Consume(ConsumeContext<StockRejected> ctx)
    {
        var order = await _db.Orders.FindAsync(ctx.Message.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();

        await _publish.Publish(new OrderCancelled
        {
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Reason = ctx.Message.Reason
        });
    }
}
