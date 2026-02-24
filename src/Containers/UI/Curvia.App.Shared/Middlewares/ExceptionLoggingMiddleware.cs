using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Curvia.App.Shared.Middlewares;

public class ExceptionLoggingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionLoggingMiddleware> _logger;

	public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
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
			_logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
			context.Response.StatusCode = 500;
			context.Response.ContentType = "application/json";

			var errorResponse = new { error = "An unexpected error occurred." };
			await context.Response.WriteAsJsonAsync(errorResponse);

			throw;
		}
	}
}

