using System.Text.Json;
using PRM.Server.Models.DTOs.Common;

namespace PRM.Server.Middleware;

public class ForcePasswordChangeMiddleware
{
	private readonly RequestDelegate _next;

	public ForcePasswordChangeMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (ShouldBlock(context))
		{
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			context.Response.ContentType = "application/json";

			var response = ApiResponse<object>.Fail("Password change required.");
			await context.Response.WriteAsync(JsonSerializer.Serialize(response));
			return;
		}

		await _next(context);
	}

	private static bool ShouldBlock(HttpContext context)
	{
		if (context.User.Identity?.IsAuthenticated != true)
		{
			return false;
		}

		var path = context.Request.Path.Value ?? string.Empty;
		if (path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var forceChange = context.User.FindFirst("force_password_change")?.Value;
		return string.Equals(forceChange, "true", StringComparison.OrdinalIgnoreCase);
	}
}
