using Cart.API.Models;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly CartStore _store;

    public CartController(CartStore store) => _store = store;

    [HttpGet("{customerId}")]
    public async Task<ActionResult<ShoppingCart>> Get(string customerId)
        => Ok(await _store.GetAsync(customerId));

    /// <summary>Add an item (or increase its quantity) in the cart.</summary>
    [HttpPost("{customerId}/items")]
    public async Task<ActionResult<ShoppingCart>> AddItem(string customerId, CartItem item)
    {
        var cart = await _store.GetAsync(customerId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);

        if (existing is null)
            cart.Items.Add(item);
        else
            existing.Quantity += item.Quantity;

        return Ok(await _store.SaveAsync(cart));
    }

    [HttpDelete("{customerId}/items/{productId:guid}")]
    public async Task<ActionResult<ShoppingCart>> RemoveItem(string customerId, Guid productId)
    {
        var cart = await _store.GetAsync(customerId);
        cart.Items.RemoveAll(i => i.ProductId == productId);
        return Ok(await _store.SaveAsync(cart));
    }

    [HttpDelete("{customerId}")]
    public async Task<IActionResult> Clear(string customerId)
    {
        await _store.DeleteAsync(customerId);
        return NoContent();
    }
}
