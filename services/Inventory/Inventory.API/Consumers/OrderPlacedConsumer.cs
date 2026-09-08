using BuildingBlocks.IntegrationEvents;
using Inventory.API.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Consumers;

/// <summary>
/// Reacts to OrderPlaced: tries to reserve stock for every line.
/// Publishes StockReserved on success or StockRejected (compensation) on failure.
/// New products default to a generous stock level so the demo flows work.
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly InventoryDbContext _db;
    private const int DefaultStock = 1000;

    public OrderPlacedConsumer(InventoryDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<OrderPlaced> ctx)
    {
        var msg = ctx.Message;

        // Ensure a stock row exists for each product (auto-provision for demo).
        foreach (var line in msg.Lines)
        {
            var stock = await _db.Stock.FirstOrDefaultAsync(s => s.ProductId == line.ProductId);
            if (stock is null)
            {
                _db.Stock.Add(new StockItem
                {
                    ProductId = line.ProductId,
                    QuantityAvailable = DefaultStock
                });
            }
        }
        await _db.SaveChangesAsync();

        // Check availability for all lines first (all-or-nothing).
        foreach (var line in msg.Lines)
        {
            var stock = await _db.Stock.FirstAsync(s => s.ProductId == line.ProductId);
            var free = stock.QuantityAvailable - stock.QuantityReserved;
            if (free < line.Quantity)
            {
                await ctx.Publish(new StockRejected
                {
                    CorrelationId = msg.CorrelationId,
                    OrderId = msg.OrderId,
                    Reason = $"Insufficient stock for {line.ProductName}."
                });
                return;
            }
        }

        // Reserve.
        foreach (var line in msg.Lines)
        {
            var stock = await _db.Stock.FirstAsync(s => s.ProductId == line.ProductId);
            stock.QuantityReserved += line.Quantity;
        }
        await _db.SaveChangesAsync();

        await ctx.Publish(new StockReserved
        {
            CorrelationId = msg.CorrelationId,
            OrderId = msg.OrderId
        });
    }
}
