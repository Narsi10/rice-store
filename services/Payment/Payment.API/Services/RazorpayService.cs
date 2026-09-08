using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Payment.API.Services;

public record RazorpayOrder(string Id, string Currency, int Amount);

/// <summary>
/// Talks to Razorpay: creates orders via their REST API and verifies the
/// payment signature returned by checkout. Keys come from configuration
/// (Razorpay:KeyId / Razorpay:KeySecret) — never hardcoded.
/// </summary>
public class RazorpayService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<RazorpayService> _log;

    public RazorpayService(HttpClient http, IConfiguration config, ILogger<RazorpayService> log)
    {
        _http = http;
        _config = config;
        _log = log;
    }

    public string KeyId => _config["Razorpay:KeyId"] ?? "";
    private string KeySecret => _config["Razorpay:KeySecret"] ?? "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(KeySecret);

    /// <summary>
    /// Creates a Razorpay order. Amount is in the smallest currency unit
    /// (paise for INR), so ₹650 -> 65000.
    /// </summary>
    public async Task<RazorpayOrder?> CreateOrderAsync(decimal amountInr, string receipt)
    {
        if (!IsConfigured)
        {
            _log.LogWarning("Razorpay not configured (missing keys).");
            return null;
        }

        var amountPaise = (int)Math.Round(amountInr * 100);
        var payload = new
        {
            amount = amountPaise,
            currency = "INR",
            receipt,
            payment_capture = 1,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{KeyId}:{KeySecret}"));
        request.Headers.Add("Authorization", $"Basic {auth}");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("Razorpay order create failed ({Status}): {Body}", (int)response.StatusCode, body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return new RazorpayOrder(
            root.GetProperty("id").GetString()!,
            root.GetProperty("currency").GetString()!,
            root.GetProperty("amount").GetInt32());
    }

    /// <summary>
    /// Verifies the checkout signature: HMAC_SHA256(order_id + "|" + payment_id,
    /// keySecret) must equal the signature Razorpay returned. This proves the
    /// payment is genuine and wasn't forged by the browser.
    /// </summary>
    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        if (!IsConfigured) return false;

        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
