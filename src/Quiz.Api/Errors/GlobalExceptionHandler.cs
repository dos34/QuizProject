using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Quiz.Api.Errors;

/// <summary>Глобальная обработка исключений: единый формат ошибок (RFC 9457 ProblemDetails).</summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException e => (e.StatusCode, "Validation failed", string.Join(" ", e.Errors)),
            AppException e => (e.StatusCode, ReasonPhrase(e.StatusCode), e.Message),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } =>
                (StatusCodes.Status409Conflict, "Conflict", "Resource already exists."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid token."),
            BadHttpRequestException e => (e.StatusCode, "Bad request", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "Something went wrong."),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogInformation("Request failed with {StatusCode}: {Detail}", status, detail);
        }

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        if (exception is ValidationException ve)
        {
            problem.Extensions["errors"] = ve.Errors;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static string ReasonPhrase(int status) =>
        Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status);
}
