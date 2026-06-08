namespace PRM.Server.Models.DTOs.Employees;

public class UpdateEmployeeDto
{
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
}
