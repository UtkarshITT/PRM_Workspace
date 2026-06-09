namespace PRM.Server.Models.DTOs.Allocations;

public class EmployeeAllocationDto
{
	public long Id { get; set; }
	public long ProjectId { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public DateOnly AllocationStartDate { get; set; }
	public DateOnly AllocationEndDate { get; set; }
	public string AllocationStatus { get; set; } = string.Empty;
}

public class EmployeeAllocationsResponseDto
{
	public IReadOnlyList<EmployeeAllocationDto> Allocations { get; set; } = [];
	public decimal TotalActiveUtilizationPercent { get; set; }
}
