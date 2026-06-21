namespace PRM.Client.Models.Manager;

public class AiSkillMatchResponse
{
	public string? Message { get; set; }
	public string? Summary { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = string.Empty;
	public List<string> GapAnalysis { get; set; } = [];
	public List<AiSkillMatchCandidate> Candidates { get; set; } = [];
}

public class AiSkillMatchCandidate
{
	public long EmployeeId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public int Rank { get; set; }
	public int MatchScore { get; set; }
	public string Reason { get; set; } = string.Empty;
	public decimal AvailabilityPercent { get; set; }
}

public class AiRiskSummaryResponse
{
	public string? Message { get; set; }
	public string? Paragraph { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = string.Empty;
	public string? ProjectName { get; set; }
	public string? HealthStatus { get; set; }
}

public class TeamBuilderRequest
{
	public long? ProjectId { get; set; }
	public string? Prompt { get; set; }
	public List<TeamRoleRequirement> Roles { get; set; } = [];
}

public class TeamRoleRequirement
{
	public string RoleTitle { get; set; } = string.Empty;
	public List<long> SkillIds { get; set; } = [];
	public string MinProficiency { get; set; } = "Beginner";
	public int AllocationPercent { get; set; } = 100;
	public int Headcount { get; set; } = 1;
}

public class TeamBuilderResponse
{
	public string? Message { get; set; }
	public string? Summary { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = string.Empty;
	public List<string> GapAnalysis { get; set; } = [];
	public List<TeamBuilderRoleResult> Roles { get; set; } = [];
}

public class TeamBuilderRoleResult
{
	public string RoleTitle { get; set; } = string.Empty;
	public int Headcount { get; set; }
	public List<AiSkillMatchCandidate> Matches { get; set; } = [];
}
