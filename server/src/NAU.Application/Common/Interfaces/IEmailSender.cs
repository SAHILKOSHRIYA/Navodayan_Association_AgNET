namespace NAU.Application.Common.Interfaces;

/// <summary>
/// Outbound email abstraction. Dev/test implementations log or capture messages;
/// the production provider (SMTP/transactional) is chosen at deployment (Phase 1 §10).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
