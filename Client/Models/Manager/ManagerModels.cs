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
}

public class TeamActiveMember
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public decimal CurrentUtilizationPercent { get; set; }
	public decimal AvailabilityPercent { get; set; }
}

public class TeamMemberDetail
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string EmploymentStatus { get; set; } = string.Empty;
	public decimal CurrentUtilizationPercent { get; set; }
	public List<string> Skills { get; set; } = [];
	public List<TeamMemberAllocation> ActiveAllocations { get; set; } = [];
	public List<string> RecentActivityTags { get; set; } = [];
}

public class TeamMemberAllocation
{
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
