using NAU.Api.Middleware;
using NAU.Application;
using NAU.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NAU API");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog from configuration (console in containers, rolling file locally).
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // API surface
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // CORS — origins come from configuration only (domain-agnostic, Phase 2 §1).
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Default", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    // Health checks: /health = liveness, /health/ready = dependencies (Phase 2 §8).
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Default")!,
            name: "postgres",
            tags: ["ready"]);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("Default");
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health", new() { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NAU API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
