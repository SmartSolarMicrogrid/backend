using System.Net;
using System.Text.Json;
using FluentValidation;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.Utilities.Exceptions;

namespace SmartSolarMicrogrid.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
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
        int statusCode;
        string title;
        string detail;
        string code;

        if (exception is DomainException domainEx)
        {
            code = domainEx.Code;
            var entry = ErrorCatalog.Get(code);
            statusCode = entry.Status;
            title = entry.Title;
            detail = domainEx.Message;
        }
        else if (exception is ValidationException valEx)
        {
            code = ErrorCodes.ValidationFailed;
            statusCode = (int)HttpStatusCode.BadRequest;
            title = "Validation failed";
            detail = string.Join("; ", valEx.Errors.Select(x => x.ErrorMessage));
        }
        else if (exception is NotFoundException notFoundEx)
        {
            code = ErrorCodes.NotFound;
            statusCode = (int)HttpStatusCode.NotFound;
            title = "Not found";
            detail = notFoundEx.Message;
        }
        else if (exception is ConflictException conflictEx)
        {
            code = conflictEx.Message.Contains("NIC", StringComparison.OrdinalIgnoreCase) ? ErrorCodes.NicExists : ErrorCodes.EmailExists;
            statusCode = (int)HttpStatusCode.Conflict;
            title = "Conflict";
            detail = conflictEx.Message;
        }
        else if (exception is UnauthorizedException unauthEx)
        {
            code = ErrorCodes.InvalidCredentials;
            statusCode = (int)HttpStatusCode.Unauthorized;
            title = "Sign-in failed";
            detail = unauthEx.Message;
        }
        else if (exception is BusinessRuleException ruleEx)
        {
            code = ErrorCodes.InvalidState;
            statusCode = (int)HttpStatusCode.UnprocessableEntity;
            title = "Business rule violated";
            detail = ruleEx.Message;
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
            code = ErrorCodes.InternalError;
            statusCode = (int)HttpStatusCode.InternalServerError;
            title = "Unexpected error";
            detail = "Something went wrong. Quote the trace ID when you report it.";
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var traceId = context.TraceIdentifier;
        var problem = new
        {
            type = $"/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            code,
            traceId
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
