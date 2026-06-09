namespace PRM.Server.Models.DTOs.Projects;

public class ManagerProjectDetailDto
{
	public long Id { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public string HealthStatus { get; set; } = string.Empty;
	public IReadOnlyList<ProjectRiskFlagDto> RiskFlags { get; set; } = [];
	public IReadOnlyList<ManagerMilestoneDto> Milestones { get; set; } = [];
	public IReadOnlyList<ProjectResourceDto> AllocatedResources { get; set; } = [];
}

public class ProjectRiskFlagDto
{
	public bool IsPositive { get; set; }
	public string Message { get; set; } = string.Empty;
}

public class ManagerMilestoneDto
{
	public long Id { get; set; }
	public int SortOrder { get; set; }
	public string MilestoneTitle { get; set; } = string.Empty;
	public DateOnly DueDate { get; set; }
	public string MilestoneStatus { get; set; } = string.Empty;
	public bool IsOverdue { get; set; }
}

public class ProjectResourceDto
{
	public string EmployeeName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public DateOnly AllocationStartDate { get; set; }
	public DateOnly AllocationEndDate { get; set; }
}
