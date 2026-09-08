using BuildingBlocks.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using OrderEntity = Order.API.Models.Order;
using Order.API.Models;

namespace Order.API.Controllers;

public record PlaceOrderItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record PlaceOrderRequest(
    string CustomerId,
    string CustomerEmail,
    List<PlaceOrderItem> Items,
    string? PaymentMethod = null,
    string? PaymentInstrument = null,
    string? TransactionId = null);
public record ReviewRequest(string ReviewedBy, string? Note);

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publish;

    public OrdersController(OrderDbContext db, IPublishEndpoint publish)
    {
        _db = db;
        _publish = publish;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderEntity>> Get(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderEntity>>> GetByCustomer(string customerId)
        => Ok(await _db.Orders.Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedOnUtc).ToListAsync());

    /// <summary>
    /// Admin: list every customer's orders, newest first. Optionally filter
    /// by status (e.g. only orders awaiting review).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderEntity>>> GetAll([FromQuery] OrderStatus? status)
    {
        var query = _db.Orders.Include(o => o.Items).AsQueryable();
        if (status is not null)
            query = query.Where(o => o.Status == status);
        return Ok(await query.OrderByDescending(o => o.CreatedOnUtc).ToListAsync());
    }

    /// <summary>
    /// Customer: delete an order that is still awaiting approval. Orders that
    /// have already been approved/rejected/cancelled cannot be deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        if (order.Status != OrderStatus.Confirmed)
            return BadRequest("Only orders awaiting approval can be cancelled.");

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Admin: approve an order for fulfillment.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderEntity>> Approve(Guid id, ReviewRequest request)
        => await Review(id, OrderStatus.Approved, request);

    /// <summary>Admin: reject an order.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderEntity>> Reject(Guid id, ReviewRequest request)
        => await Review(id, OrderStatus.Rejected, request);

    private async Task<ActionResult<OrderEntity>> Review(
        Guid id, OrderStatus newStatus, ReviewRequest request)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        order.Status = newStatus;
        order.ReviewedBy = request.ReviewedBy;
        order.ReviewedOnUtc = DateTime.UtcNow;
        order.ReviewNote = request.Note;
        order.UpdatedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Publish an event so the Notification service can tell the customer.
        if (newStatus == OrderStatus.Approved)
        {
            await _publish.Publish(new OrderApproved
            {
                CorrelationId = order.Id,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CustomerEmail = order.CustomerEmail
            });
        }
        else if (newStatus == OrderStatus.Rejected)
        {
            await _publish.Publish(new OrderRejected
            {
                CorrelationId = order.Id,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CustomerEmail = order.CustomerEmail,
                Reason = request.Note ?? "No reason provided."
            });
        }

        return Ok(order);
    }

    /// <summary>
    /// Places an order and kicks off the checkout saga by publishing OrderPlaced.
    /// Inventory and Payment react asynchronously; the order stays Pending until
    /// both succeed (then Confirmed) or one fails (then Cancelled).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderEntity>> Place(PlaceOrderRequest request)
    {
        var order = new OrderEntity
        {
            CustomerId = request.CustomerId,
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            PaymentInstrument = request.PaymentInstrument,
            TransactionId = request.TransactionId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _publish.Publish(new OrderPlaced
        {
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Lines = order.Items
                .Select(i => new OrderLine(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
                .ToList()
        });

        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }
}
