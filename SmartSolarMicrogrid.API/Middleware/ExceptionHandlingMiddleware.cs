using System.Net;
using System.Text.Json;
using FluentValidation;
using SmartSolarMicrogrid.API.Utilities.Exceptions;

namespace SmartSolarMicrogrid.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException      e => (HttpStatusCode.NotFound,                 "Not Found",              e.Message),
            UnauthorizedException  e => (HttpStatusCode.Unauthorized,             "Unauthorized",           e.Message),
            ConflictException      e => (HttpStatusCode.Conflict,                 "Conflict",               e.Message),
            BusinessRuleException  e => (HttpStatusCode.UnprocessableEntity,      "Business Rule Violated", e.Message),
            ValidationException    e => (HttpStatusCode.BadRequest,               "Validation Failed",      string.Join("; ", e.Errors.Select(x => x.ErrorMessage))),
            _                        => (HttpStatusCode.InternalServerError,      "Server Error",           "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            title,
            detail,
            traceId = context.TraceIdentifier,
        });

        await context.Response.WriteAsync(body);
    }
}
