namespace PRM.Server.Models.DTOs.Common;

public class ApiResponse<T>
{
	public bool Success { get; set; }
	public T? Data { get; set; }
	public string? Message { get; set; }
	public string? Error { get; set; }
	public List<string> Details { get; set; } = [];

	public static ApiResponse<T> Ok(T data, string message) =>
		new() { Success = true, Data = data, Message = message };

	public static ApiResponse<T> Fail(string error, List<string>? details = null) =>
		new() { Success = false, Error = error, Details = details ?? [] };
}
