using MealPreparator.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using ApplicationValidationException = MealPreparator.Application.Exceptions.ValidationException;

namespace MealPreparator.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            DomainValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation error",
                exception.Message),
            ApplicationValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation error",
                exception.Message),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            MealPreparator.Application.Exceptions.NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),
            MealPreparator.Application.Exceptions.ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with status {StatusCode} for {Method} {Path}.",
                statusCode, context.Request.Method, context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
