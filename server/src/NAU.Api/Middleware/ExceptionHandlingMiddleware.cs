using FluentValidation;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Models;

namespace NAU.Api.Middleware;

/// <summary>
/// Global exception handler: maps known exception types to the standard envelope
/// with the correct HTTP status (Phase 2 §6). Unknown exceptions are logged with
/// their correlation id and returned as an opaque 500 — internals never leak.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, response) = Map(ex, context);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private (int Status, ApiResponse<object>) Map(Exception ex, HttpContext context) => ex switch
    {
        ValidationException ve => (StatusCodes.Status400BadRequest,
            ApiResponse<object>.Fail("Validation failed.",
                ve.Errors.Select(e => new ApiError(e.PropertyName, e.ErrorCode ?? "INVALID", e.ErrorMessage)).ToList())),

        NotFoundException nf => (StatusCodes.Status404NotFound, ApiResponse<object>.Fail(nf.Message)),
        ForbiddenException fb => (StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(fb.Message)),
        ConflictException cf => (StatusCodes.Status409Conflict, ApiResponse<object>.Fail(cf.Message)),
        DomainRuleException dr => (StatusCodes.Status422UnprocessableEntity, ApiResponse<object>.Fail(dr.Message)),

        _ => LogAndReturn500(ex, context)
    };

    private (int, ApiResponse<object>) LogAndReturn500(Exception ex, HttpContext context)
    {
        logger.LogError(ex, "Unhandled exception for {Method} {Path} (TraceId: {TraceId})",
            context.Request.Method, context.Request.Path, context.TraceIdentifier);

        return (StatusCodes.Status500InternalServerError,
            ApiResponse<object>.Fail($"An unexpected error occurred. Reference: {context.TraceIdentifier}"));
    }
}
