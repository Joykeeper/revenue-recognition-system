using System.Net;
using Project.Exceptions;

namespace Project.Middlewares;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string message;

        switch (exception)
        {
            case NotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case ConflictException:
                statusCode = (int)HttpStatusCode.Conflict;
                message = exception.Message;
                break;
            case BadRequestException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Bad request access"; //exception.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = "Unauthorized access";
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "Server error";
                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message,
                type = exception.GetType().Name
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}