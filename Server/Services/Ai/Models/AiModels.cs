namespace PRM.Server.Services.Ai.Models;

public class AiCandidateContext
{
	public long Id { get; init; }
	public string FullName { get; init; } = string.Empty;
	public string? Department { get; init; }
	public string? ManagerName { get; init; }
	public string EmploymentStatus { get; init; } = string.Empty;
	public decimal CurrentUtilization { get; init; }
	public decimal AvailabilityPercent { get; init; }
	public IReadOnlyList<string> Skills { get; init; } = [];
	public IReadOnlyList<string> RecentActivityTags { get; init; } = [];
}

public class AiSkillMatchPromptContext
{
	public string Requirement { get; init; } = string.Empty;
	public string Scope { get; init; } = "Organization";
	public IReadOnlyList<AiCandidateJsonContext> Candidates { get; init; } = [];
}

public class AiTeamBuilderPromptContext
{
	public string ManagerRequest { get; init; } = string.Empty;
	public string Scope { get; init; } = "Organization";
	public IReadOnlyList<AiCandidateJsonContext> FullyAvailableCandidates { get; init; } = [];
	public IReadOnlyList<AiCandidateJsonContext> PartiallyAvailableCandidates { get; init; } = [];
	public IReadOnlyList<AiCandidateJsonContext> FullyAllocatedCandidates { get; init; } = [];
}

public class AiCandidateJsonContext
{
	public long EmployeeId { get; init; }
	public string FullName { get; init; } = string.Empty;
	public string? Department { get; init; }
	public string? ManagerName { get; init; }
	public string EmploymentStatus { get; init; } = string.Empty;
	public decimal CurrentUtilizationPercent { get; init; }
	public decimal AvailabilityPercent { get; init; }
	public IReadOnlyList<string> Skills { get; init; } = [];
	public IReadOnlyList<string> RecentActivityTags { get; init; } = [];
}

public class ProjectAiContext
{
	public string ProjectName { get; init; } = string.Empty;
	public string HealthStatus { get; init; } = string.Empty;
	public DateOnly EndDate { get; init; }
	public IReadOnlyList<ProjectMilestoneAiContext> Milestones { get; init; } = [];
	public IReadOnlyList<ProjectHoursAiContext> LastWeekHours { get; init; } = [];
	public IReadOnlyList<string> RiskFlags { get; init; } = [];
}

public class ProjectMilestoneAiContext
{
	public string Title { get; init; } = string.Empty;
	public DateOnly DueDate { get; init; }
	public string Status { get; init; } = string.Empty;
	public bool IsOverdue { get; init; }
}

public class ProjectHoursAiContext
{
	public string EmployeeName { get; init; } = string.Empty;
	public decimal LoggedHours { get; init; }
	public decimal ExpectedHours { get; init; }
}

public class TeamRolePromptContext
{
	public string RoleTitle { get; init; } = string.Empty;
	public IReadOnlyList<string> SkillNames { get; init; } = [];
	public string MinProficiency { get; init; } = "Beginner";
	public int AllocationPercent { get; init; }
	public int Headcount { get; init; }
}

public record LlmSettings(string Provider, string ApiKey);
