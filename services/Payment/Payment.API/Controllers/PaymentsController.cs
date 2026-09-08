using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _db;
    public PaymentsController(PaymentDbContext db) => _db = db;

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<PaymentRecord>> GetByOrder(Guid orderId)
    {
        var record = await _db.Payments
            .OrderByDescending(p => p.CreatedOnUtc)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
        return record is null ? NotFound() : Ok(record);
    }
}
