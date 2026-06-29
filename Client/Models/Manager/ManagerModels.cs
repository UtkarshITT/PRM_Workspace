namespace PRM.Client.Models.Manager;

public class TeamDashboardResponse
{
	public List<TeamBenchMember> BenchMembers { get; set; } = [];
	public List<TeamActiveMember> ActiveMembers { get; set; } = [];
}

public class TeamBenchMember
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public List<string> Skills { get; set; } = [];
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
}

public class TeamActiveMember
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public decimal CurrentUtilizationPercent { get; set; }
	public decimal AvailabilityPercent { get; set; }
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
}

public class TeamMemberDetail
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string EmploymentStatus { get; set; } = string.Empty;
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
	public decimal CurrentUtilizationPercent { get; set; }
	public List<string> Skills { get; set; } = [];
	public List<TeamMemberAllocation> ActiveAllocations { get; set; } = [];
	public List<string> RecentActivityTags { get; set; } = [];
}

public class TeamMemberAllocation
{
	public long AllocationId { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public string AllocationStartDate { get; set; } = string.Empty;
	public string AllocationEndDate { get; set; } = string.Empty;
}

public class CreateAllocationRequest
{
	public long EmployeeId { get; set; }
	public long ProjectId { get; set; }
	public decimal AllocationPercentage { get; set; }
	public string AllocationStartDate { get; set; } = string.Empty;
	public string AllocationEndDate { get; set; } = string.Empty;
}

public class AllocationCreatedResponse
{
	public long AllocationId { get; set; }
	public long EmployeeId { get; set; }
	public long ProjectId { get; set; }
	public decimal AllocationPercentage { get; set; }
	public string AllocationStatus { get; set; } = string.Empty;
	public string EmploymentStatus { get; set; } = string.Empty;
}

public class ManagerProjectListItem
{
	public long Id { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public string EndDate { get; set; } = string.Empty;
	public string HealthStatus { get; set; } = string.Empty;
	public string ProjectStatus { get; set; } = string.Empty;
}

public class ManagerProjectDetail
{
	public long Id { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public string HealthStatus { get; set; } = string.Empty;
	public List<ProjectRiskFlag> RiskFlags { get; set; } = [];
	public List<ManagerMilestone> Milestones { get; set; } = [];
	public List<ProjectResource> AllocatedResources { get; set; } = [];
}

public class ProjectRiskFlag
{
	public bool IsPositive { get; set; }
	public string Message { get; set; } = string.Empty;
}

public class ManagerMilestone
{
	public long Id { get; set; }
	public int SortOrder { get; set; }
	public string MilestoneTitle { get; set; } = string.Empty;
	public string DueDate { get; set; } = string.Empty;
	public string MilestoneStatus { get; set; } = string.Empty;
	public bool IsOverdue { get; set; }
}

public class ProjectResource
{
	public string EmployeeName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public string AllocationStartDate { get; set; } = string.Empty;
	public string AllocationEndDate { get; set; } = string.Empty;
}

public class TeamTimesheetRow
{
	public long TimesheetId { get; set; }
	public string EmployeeName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public decimal HoursLogged { get; set; }
	public string Status { get; set; } = string.Empty;
}

public class ManagerTimesheetDetail
{
	public long Id { get; set; }
	public string WeekStartDate { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public decimal TotalHours { get; set; }
	public string? Remarks { get; set; }
	public List<ManagerTimesheetLineItem> LineItems { get; set; } = [];
}

public class ManagerTimesheetLineItem
{
	public string ProjectName { get; set; } = string.Empty;
	public decimal HoursLogged { get; set; }
	public List<string> ActivityTags { get; set; } = [];
}
