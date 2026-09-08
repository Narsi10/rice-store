namespace Notification.API.Services;

/// <summary>Sends transactional emails to customers.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
