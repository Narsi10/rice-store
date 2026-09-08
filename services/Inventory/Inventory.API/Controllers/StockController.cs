using Inventory.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers;

public record SetStockRequest(Guid ProductId, int QuantityAvailable);

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly InventoryDbContext _db;
    public StockController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockItem>>> GetAll()
        => Ok(await _db.Stock.ToListAsync());

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<StockItem>> Get(Guid productId)
    {
        var stock = await _db.Stock.FirstOrDefaultAsync(s => s.ProductId == productId);
        return stock is null ? NotFound() : Ok(stock);
    }

    /// <summary>Admin endpoint to set or replenish stock for a product.</summary>
    [HttpPut]
    public async Task<ActionResult<StockItem>> Set(SetStockRequest request)
    {
        var stock = await _db.Stock.FirstOrDefaultAsync(s => s.ProductId == request.ProductId);
        if (stock is null)
        {
            stock = new StockItem { ProductId = request.ProductId };
            _db.Stock.Add(stock);
        }
        stock.QuantityAvailable = request.QuantityAvailable;
        await _db.SaveChangesAsync();
        return Ok(stock);
    }
}
