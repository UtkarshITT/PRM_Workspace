namespace PRM.Server.Models.DTOs.Employees;

public class EmployeeListItemDto
{
	public long Id { get; set; }
	public string EmployeeCode { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
	public string EmploymentStatus { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public long? ManagerId { get; set; }
	public string? ManagerName { get; set; }
	public IReadOnlyList<string> Skills { get; set; } = [];
}
