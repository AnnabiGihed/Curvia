using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Curvia.App.Shared.Middlewares;

public class LoggingMiddleware
{
	#region Properties
	protected readonly RequestDelegate _next;
	protected readonly ILogger<LoggingMiddleware> _logger;
	#endregion

	#region Constructor
	public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}
	#endregion

	#region Methods
	public async Task InvokeAsync(HttpContext context)
	{
		_logger.LogInformation("Handling request: {Path}", context.Request.Path);
		await _next(context);
		_logger.LogInformation("Finished handling request.");
	}
	#endregion
}