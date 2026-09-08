using BuildingBlocks.IntegrationEvents;
using MassTransit;
using Order.API.Data;
using Order.API.Models;

namespace Order.API.Consumers;

/// <summary>Payment completed -> confirm the order (happy path end).</summary>
public class PaymentCompletedConsumer : IConsumer<PaymentCompleted>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publish;

    public PaymentCompletedConsumer(OrderDbContext db, IPublishEndpoint publish)
    {
        _db = db;
        _publish = publish;
    }

    public async Task Consume(ConsumeContext<PaymentCompleted> ctx)
    {
        var order = await _db.Orders.FindAsync(ctx.Message.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync();

        await _publish.Publish(new OrderConfirmed
        {
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail
        });
    }
}

/// <summary>Payment failed -> cancel the order (compensation).</summary>
public class PaymentFailedConsumer : IConsumer<PaymentFailed>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publish;

    public PaymentFailedConsumer(OrderDbContext db, IPublishEndpoint publish)
    {
        _db = db;
        _publish = publish;
    }

    public async Task Consume(ConsumeContext<PaymentFailed> ctx)
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
