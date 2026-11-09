namespace Quiz.Api.Errors;

/// <summary>Ожидаемые ошибки бизнес-логики. GlobalExceptionHandler превращает их в ProblemDetails.</summary>
public abstract class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string message) : AppException(StatusCodes.Status404NotFound, message);

public sealed class ForbiddenException(string message) : AppException(StatusCodes.Status403Forbidden, message);

public sealed class ConflictException(string message) : AppException(StatusCodes.Status409Conflict, message);

public sealed class UnauthorizedException(string message) : AppException(StatusCodes.Status401Unauthorized, message);

public sealed class ValidationException(IReadOnlyList<string> errors)
    : AppException(StatusCodes.Status400BadRequest, "Validation failed")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
