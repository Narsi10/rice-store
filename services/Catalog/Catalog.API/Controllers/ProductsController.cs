using Catalog.API.Data;
using Catalog.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public ProductsController(CatalogDbContext db) => _db = db;

    /// <summary>List products, optionally filtered by rice type or search term.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(
        [FromQuery] RiceType? riceType,
        [FromQuery] string? search)
    {
        var query = _db.Products.Where(p => p.IsActive).AsQueryable();

        if (riceType is not null)
            query = query.Where(p => p.RiceType == riceType);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search));

        return Ok(await query.OrderBy(p => p.Name).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Bulk price lookup used by the Order service at checkout so it can
    /// trust catalog prices rather than prices sent from the browser.
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<IEnumerable<Product>>> GetBatch([FromBody] Guid[] ids)
    {
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, Product input)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Name = input.Name;
        product.Description = input.Description;
        product.RiceType = input.RiceType;
        product.Brand = input.Brand;
        product.Price = input.Price;
        product.CostPrice = input.CostPrice;
        product.Stock = input.Stock;
        product.WeightKg = input.WeightKg;
        product.ImageUrl = input.ImageUrl;
        product.IsActive = input.IsActive;
        product.UpdatedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.IsActive = false; // soft delete keeps order history intact
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
