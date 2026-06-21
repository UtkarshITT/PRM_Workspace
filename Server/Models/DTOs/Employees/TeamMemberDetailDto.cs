namespace PRM.Server.Models.DTOs.Employees;

public class TeamMemberDetailDto
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string EmploymentStatus { get; set; } = string.Empty;
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
	public decimal CurrentUtilizationPercent { get; set; }
	public IReadOnlyList<string> Skills { get; set; } = [];
	public IReadOnlyList<TeamMemberAllocationDto> ActiveAllocations { get; set; } = [];
	public IReadOnlyList<string> RecentActivityTags { get; set; } = [];
}

public class TeamMemberAllocationDto
{
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public DateOnly AllocationStartDate { get; set; }
	public DateOnly AllocationEndDate { get; set; }
}
