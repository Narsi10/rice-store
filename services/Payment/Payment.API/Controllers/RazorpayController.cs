using Microsoft.AspNetCore.Mvc;
using Payment.API.Data;
using Payment.API.Services;

namespace Payment.API.Controllers;

public record CreateOrderRequest(decimal AmountInr, string Receipt);
public record VerifyRequest(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    Guid OrderId,
    decimal AmountInr);

[ApiController]
[Route("api/payments/razorpay")]
public class RazorpayController : ControllerBase
{
    private readonly RazorpayService _razorpay;
    private readonly PaymentDbContext _db;
    private readonly ILogger<RazorpayController> _log;

    public RazorpayController(RazorpayService razorpay, PaymentDbContext db, ILogger<RazorpayController> log)
    {
        _razorpay = razorpay;
        _db = db;
        _log = log;
    }

    /// <summary>Tells the frontend whether real Razorpay is enabled + the public key id.</summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
        => Ok(new { enabled = _razorpay.IsConfigured, keyId = _razorpay.KeyId });

    /// <summary>Step 1: create a Razorpay order the checkout widget will pay against.</summary>
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var order = await _razorpay.CreateOrderAsync(request.AmountInr, request.Receipt);
        if (order is null)
            return BadRequest("Razorpay is not configured or order creation failed.");

        return Ok(new
        {
            keyId = _razorpay.KeyId,
            orderId = order.Id,
            amount = order.Amount,
            currency = order.Currency,
        });
    }

    /// <summary>
    /// Step 2: verify the signature returned by checkout. Only a valid signature
    /// (computed with the secret key) is accepted, then we record the payment.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyRequest request)
    {
        var valid = _razorpay.VerifySignature(
            request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);

        _db.Payments.Add(new PaymentRecord
        {
            OrderId = request.OrderId,
            Amount = request.AmountInr,
            Status = valid ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            TransactionId = request.RazorpayPaymentId,
        });
        await _db.SaveChangesAsync();

        if (!valid)
        {
            _log.LogWarning("Razorpay signature verification FAILED for order {OrderId}", request.OrderId);
            return BadRequest(new { verified = false });
        }

        return Ok(new { verified = true, transactionId = request.RazorpayPaymentId });
    }
}
