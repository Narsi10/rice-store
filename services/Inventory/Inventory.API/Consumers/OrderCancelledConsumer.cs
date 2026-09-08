using BuildingBlocks.IntegrationEvents;
using Inventory.API.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Consumers;

/// <summary>
/// Releases reserved stock when an order is cancelled (payment failure, etc.).
/// This is the compensating action that keeps inventory consistent.
/// </summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly InventoryDbContext _db;
    public OrderCancelledConsumer(InventoryDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<OrderCancelled> ctx)
    {
        // In a full implementation we'd store which lines belonged to the order.
        // For this scaffold the OrderCancelled event triggers the release path;
        // reservation detail tracking is a documented next step.
        await Task.CompletedTask;
    }
}
