namespace PRM.Server.Models.DTOs.Employees;

public class TeamDashboardDto
{
	public IReadOnlyList<TeamBenchMemberDto> BenchMembers { get; set; } = [];
	public IReadOnlyList<TeamActiveMemberDto> ActiveMembers { get; set; } = [];
}

public class TeamBenchMemberDto
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public IReadOnlyList<string> Skills { get; set; } = [];
}

public class TeamActiveMemberDto
{
	public long Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public decimal CurrentUtilizationPercent { get; set; }
	public decimal AvailabilityPercent { get; set; }
}
