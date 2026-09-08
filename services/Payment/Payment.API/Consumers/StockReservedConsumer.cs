using BuildingBlocks.IntegrationEvents;
using MassTransit;
using Payment.API.Data;

namespace Payment.API.Consumers;

/// <summary>
/// Once stock is reserved, charge the customer. This scaffold simulates a
/// gateway call (always succeeds unless amount is absurd). Swap the
/// SimulateCharge method for Stripe/Razorpay/PayPal in production.
/// </summary>
public class StockReservedConsumer : IConsumer<StockReserved>
{
    private readonly PaymentDbContext _db;

    public StockReservedConsumer(PaymentDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<StockReserved> ctx)
    {
        // In a real system we'd look up the order amount from the message or a
        // read model. StockReserved carries the OrderId; here we simulate success.
        var success = SimulateCharge(out var transactionId, out var reason);

        _db.Payments.Add(new PaymentRecord
        {
            OrderId = ctx.Message.OrderId,
            Amount = 0m, // populated from order read-model in production
            Status = success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            TransactionId = transactionId
        });
        await _db.SaveChangesAsync();

        if (success)
        {
            await ctx.Publish(new PaymentCompleted
            {
                CorrelationId = ctx.Message.CorrelationId,
                OrderId = ctx.Message.OrderId,
                TransactionId = transactionId
            });
        }
        else
        {
            await ctx.Publish(new PaymentFailed
            {
                CorrelationId = ctx.Message.CorrelationId,
                OrderId = ctx.Message.OrderId,
                Reason = reason
            });
        }
    }

    private static bool SimulateCharge(out string transactionId, out string reason)
    {
        transactionId = $"txn_{Guid.NewGuid():N}";
        reason = string.Empty;
        return true; // simulated gateway approval
    }
}
