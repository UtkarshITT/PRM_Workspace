namespace PRM.Server.Models.DTOs.Allocations;

public class AllocationListItemDto
{
	public long Id { get; set; }
	public string EmployeeName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public DateOnly AllocationStartDate { get; set; }
	public DateOnly AllocationEndDate { get; set; }
	public string AllocationStatus { get; set; } = string.Empty;
}
