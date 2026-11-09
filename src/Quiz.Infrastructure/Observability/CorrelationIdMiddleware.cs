using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Quiz.Infrastructure.Observability;

/// <summary>
/// Берёт X-Correlation-Id из запроса (или генерирует), кладёт в LogContext и возвращает в ответе.
/// TraceId/SpanId Serilog берёт из Activity сам.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
