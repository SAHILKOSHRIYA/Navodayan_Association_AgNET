namespace NAU.Application.Common.Models;

/// <summary>
/// Standard response envelope returned by every API endpoint (Phase 2 §6).
/// </summary>
public sealed record ApiResponse<T>(bool Success, T? Data, string? Message, IReadOnlyList<ApiError>? Errors)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message, null);

    public static ApiResponse<T> Fail(string message, IReadOnlyList<ApiError>? errors = null) =>
        new(false, default, message, errors);
}

public sealed record ApiError(string? Field, string Code, string Message);
