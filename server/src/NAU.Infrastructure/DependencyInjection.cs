using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NAU.Application.Common.Interfaces;
using NAU.Application.Features.Admin;
using NAU.Infrastructure.Admin;
using NAU.Infrastructure.Auth;
using NAU.Infrastructure.Email;
using NAU.Infrastructure.Identity;
using NAU.Infrastructure.Payments;
using NAU.Infrastructure.Persistence;
using NAU.Infrastructure.Storage;

namespace NAU.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = NormalizeConnectionString(
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured."));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ASP.NET Identity (password policy per Phase 2 §7; lockout enabled).
        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireUppercase = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, ConsoleEmailSender>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Payment gateway: live Razorpay when configured, deterministic test gateway otherwise.
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        var provider = configuration[$"{PaymentOptions.SectionName}:Provider"] ?? "test";
        if (string.Equals(provider, "razorpay", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IPaymentGateway, RazorpayGateway>();
        else
            services.AddScoped<IPaymentGateway, TestPaymentGateway>();

        return services;
    }

    /// <summary>
    /// Accepts either a native Npgsql key=value string or a postgres:// URL (as provided by
    /// Render/Heroku) and returns a valid Npgsql connection string. URLs get SSL enabled, which
    /// managed Postgres hosts require.
    /// </summary>
    internal static string NormalizeConnectionString(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = true,
        };
        return builder.ToString();
    }
}
