using Microsoft.Extensions.Logging;
using NAU.Application.Common.Interfaces;

namespace NAU.Infrastructure.Email;

/// <summary>
/// Development email sender — writes messages to the log instead of sending.
/// The production provider (SMTP/transactional) replaces this via DI at deployment.
/// </summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("EMAIL (dev) → {To} | {Subject}\n{Body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
