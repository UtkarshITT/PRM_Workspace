namespace PRM.Server.Middleware;

public class CorrelationIdMiddleware
{
	public const string HeaderName = "X-Correlation-Id";
	public const string ItemName = "CorrelationId";

	private readonly RequestDelegate _next;
	private readonly ILogger<CorrelationIdMiddleware> _logger;

	public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
		if (string.IsNullOrWhiteSpace(correlationId))
		{
			correlationId = Guid.NewGuid().ToString("N");
		}

		context.Items[ItemName] = correlationId;
		context.Response.Headers[HeaderName] = correlationId;

		using (_logger.BeginScope(new Dictionary<string, object>
		{
			["CorrelationId"] = correlationId,
			["RequestPath"] = context.Request.Path.Value ?? string.Empty
		}))
		{
			await _next(context);
		}
	}
}
