namespace NAU.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"console" (dev — logs emails) or "smtp" (real delivery via a provider).</summary>
    public string Provider { get; init; } = "console";
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromEmail { get; init; } = "noreply@nau.local";
    public string FromName { get; init; } = "Navodaya Alumni Association";
    public bool UseStartTls { get; init; } = true;
}
