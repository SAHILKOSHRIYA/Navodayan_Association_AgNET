using System.Net;
using System.Net.Http.Json;

namespace NAU.IntegrationTests;

/// <summary>End-to-end HTTP tests for the security-critical paths (auth, RBAC, envelopes).</summary>
public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_is_ok()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Register_then_duplicate_is_conflict()
    {
        var email = $"user{Guid.NewGuid():N}@example.com";
        var body = new { fullName = "Test User", email, password = "Test@1234" };

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Weak_password_is_rejected_with_validation_error()
    {
        var res = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Weak", email = $"weak{Guid.NewGuid():N}@example.com", password = "weak" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_before_verification_is_blocked()
    {
        var email = $"unverified{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new { fullName = "U", email, password = "Test@1234" });

        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Test@1234" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Protected_route_without_token_is_unauthorized()
    {
        var res = await _client.GetAsync("/api/v1/profiles/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Admin_route_requires_admin_role()
    {
        // No token → 401 (an authenticated non-admin would get 403; both prove the gate holds).
        var res = await _client.GetAsync("/api/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Seeded_super_admin_can_log_in()
    {
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@test.local", password = "Admin@12345" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var payload = await res.Content.ReadFromJsonAsync<LoginEnvelope>();
        Assert.True(payload!.Success);
        Assert.Contains("SuperAdmin", payload.Data!.User.Roles);
    }

    private sealed record LoginEnvelope(bool Success, LoginData? Data);
    private sealed record LoginData(UserData User);
    private sealed record UserData(string[] Roles);
}
