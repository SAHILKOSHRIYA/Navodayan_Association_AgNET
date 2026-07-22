using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace NAU.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container (Testcontainers). Migrations and the
/// admin/school/role seed run on startup, so tests exercise the genuine end-to-end configuration.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _db.GetConnectionString(),
            ["Database:MigrateOnStartup"] = "true",
            ["Jwt:Secret"] = "integration-test-secret-0123456789abcdef0123456789abcdef",
            ["Seed:SuperAdmin:Email"] = "admin@test.local",
            ["Seed:SuperAdmin:Password"] = "Admin@12345",
            ["Seed:SuperAdmin:FullName"] = "Test Admin",
            ["Payments:Provider"] = "test",
            ["Payments:KeySecret"] = "test_secret",
            ["Payments:WebhookSecret"] = "test_webhook_secret",
        }));

        return base.CreateHost(builder);
    }
}
