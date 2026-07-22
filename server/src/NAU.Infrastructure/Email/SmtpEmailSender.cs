using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NAU.Application.Common.Interfaces;

namespace NAU.Infrastructure.Email;

/// <summary>
/// Production email sender over SMTP (Brevo, Gmail, Amazon SES, …). Selected when
/// Email:Provider == "smtp". A send failure is logged but does not throw — a bounced
/// verification email must never break a user's registration request.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _opt = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var secure = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_opt.Host, _opt.Port, secure, cancellationToken);
            if (!string.IsNullOrEmpty(_opt.Username))
                await client.AuthenticateAsync(_opt.Username, _opt.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}: {Subject}", toEmail, subject);
        }
    }
}
