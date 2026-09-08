namespace Notification.API.Services;

/// <summary>Simple HTML templates for the transactional emails.</summary>
public static class EmailTemplates
{
    private static string Wrap(string title, string body, string accent) => $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;border:1px solid #eee;border-radius:12px;overflow:hidden'>
  <div style='background:{accent};color:#fff;padding:18px 24px;font-size:20px;font-weight:700'>🌾 Rice Store</div>
  <div style='padding:24px'>
    <h2 style='margin-top:0'>{title}</h2>
    {body}
    <p style='color:#888;font-size:13px;margin-top:28px'>Thank you for shopping with Rice Store.</p>
  </div>
</div>";

    public static (string Subject, string Html) Confirmed(Guid orderId) =>
        ($"Order #{Short(orderId)} confirmed",
         Wrap("Your order is confirmed 🎉",
              $"<p>Your rice order <strong>#{Short(orderId)}</strong> has been received and is now awaiting approval. We'll email you once it's approved.</p>",
              "#4c8b3f"));

    public static (string Subject, string Html) Approved(Guid orderId) =>
        ($"Order #{Short(orderId)} approved",
         Wrap("Good news — your order is approved ✅",
              $"<p>Your order <strong>#{Short(orderId)}</strong> has been <strong>approved</strong> and is being prepared for delivery.</p>",
              "#4c8b3f"));

    public static (string Subject, string Html) Rejected(Guid orderId, string reason) =>
        ($"Order #{Short(orderId)} update",
         Wrap("Your order was rejected",
              $"<p>We're sorry — your order <strong>#{Short(orderId)}</strong> was <strong>rejected</strong>.</p><p>Reason: {reason}</p>",
              "#c0392b"));

    public static (string Subject, string Html) Cancelled(Guid orderId, string reason) =>
        ($"Order #{Short(orderId)} cancelled",
         Wrap("Your order was cancelled",
              $"<p>Your order <strong>#{Short(orderId)}</strong> was cancelled.</p><p>Reason: {reason}</p>",
              "#c0392b"));

    private static string Short(Guid id) => id.ToString()[..8].ToUpperInvariant();
}
