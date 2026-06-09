using System.Text.Json;
using PRM.Server.Models.DTOs.Common;

namespace PRM.Server.Middleware;

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
		catch (Exceptions.ValidationException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message, ex.Details);
		}
		catch (Exceptions.OverAllocationException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
		}
		catch (Exceptions.UnauthorizedAppException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
		}
		catch (Exceptions.NotFoundException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
		}
		catch (Exceptions.ConflictException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
		}
		catch (Exceptions.DuplicateTimesheetException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
		}
		catch (Exceptions.FutureWeekException ex)
		{
			await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception");
			await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
		}
	}

	private static async Task WriteErrorAsync(
		HttpContext context,
		int statusCode,
		string error,
		IReadOnlyList<string>? details = null)
	{
		context.Response.StatusCode = statusCode;
		context.Response.ContentType = "application/json";

		var response = ApiResponse<object>.Fail(error, details?.ToList());
		await context.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}
