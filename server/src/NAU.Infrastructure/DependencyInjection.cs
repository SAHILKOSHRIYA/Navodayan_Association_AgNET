using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NAU.Application.Common.Interfaces;
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
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

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
}
