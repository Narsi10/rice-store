using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Notification.API.Services;

/// <summary>
/// Sends email through Brevo's SMTP relay using MailKit, which handles the
/// STARTTLS handshake correctly (unlike the legacy System.Net.Mail.SmtpClient).
/// Configured via the "Email" section of appsettings / environment variables.
/// If no SMTP key is present it logs the message instead of sending.
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoEmailSender> _log;

    public BrevoEmailSender(IConfiguration config, ILogger<BrevoEmailSender> log)
    {
        _config = config;
        _log = log;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpKey = _config["Email:SmtpKey"];
        var smtpLogin = _config["Email:SmtpLogin"];       // Brevo SMTP login
        var fromEmail = _config["Email:FromEmail"] ?? smtpLogin ?? "no-reply@ricestore.test";
        var fromName = _config["Email:FromName"] ?? "Rice Store";
        var host = _config["Email:SmtpHost"] ?? "smtp-relay.brevo.com";
        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;

        if (string.IsNullOrWhiteSpace(smtpKey) || string.IsNullOrWhiteSpace(smtpLogin))
        {
            _log.LogWarning(
                "[Email disabled - missing SMTP login/key] Would send to {To}: {Subject}",
                toEmail, subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            // Port 587 uses STARTTLS; MailKit negotiates it properly.
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpLogin, smtpKey);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _log.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error sending email to {To}", toEmail);
        }
    }
}
