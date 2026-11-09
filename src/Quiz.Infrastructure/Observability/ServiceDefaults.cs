using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Quiz.Infrastructure.Observability;

public static class ServiceDefaults
{
    /// <summary>Общая обвязка для всех сервисов: Serilog (JSON в stdout) и health checks.</summary>
    public static WebApplicationBuilder AddQuizServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        var jsonLogs = builder.Configuration.GetValue("Logging:Json", !builder.Environment.IsDevelopment());

        builder.Services.AddSerilog((services, lc) =>
        {
            lc.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName);

            if (jsonLogs)
            {
                lc.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
            else
            {
                lc.WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            }
        });

        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication UseQuizServiceDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(o =>
        {
            // health-пробы Kubernetes не засоряют логи
            o.GetLevel = (ctx, _, ex) =>
                ex is not null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
                : ctx.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : LogEventLevel.Information;
        });
        return app;
    }

    /// <summary>/health/live — процесс жив; /health/ready — доступны зависимости (Mongo, RabbitMQ).</summary>
    public static WebApplication MapQuizHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
        return app;
    }
}
