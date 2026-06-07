namespace PRM.Client.Models;

public class ApiResponse<T>
{
	public bool Success { get; set; }
	public T? Data { get; set; }
	public string? Message { get; set; }
	public string? Error { get; set; }
	public List<string> Details { get; set; } = [];
}
