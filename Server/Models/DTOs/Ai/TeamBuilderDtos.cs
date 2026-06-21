namespace PRM.Server.Models.DTOs.Ai;

public class TeamBuilderRequestDto
{
	public long? ProjectId { get; set; }
	public string? Prompt { get; set; }
	public List<TeamRoleRequirementDto> Roles { get; set; } = [];
}

public class TeamRoleRequirementDto
{
	public string RoleTitle { get; set; } = string.Empty;
	public List<long> SkillIds { get; set; } = [];
	public string MinProficiency { get; set; } = "Beginner";
	public int AllocationPercent { get; set; } = 100;
	public int Headcount { get; set; } = 1;
}

public class TeamBuilderResponseDto
{
	public string? Message { get; set; }
	public string? Summary { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = "AI-generated. Verify before confirming.";
	public List<string> GapAnalysis { get; set; } = [];
	public List<TeamBuilderRoleResultDto> Roles { get; set; } = [];
}

public class TeamBuilderRoleResultDto
{
	public string RoleTitle { get; set; } = string.Empty;
	public int Headcount { get; set; }
	public List<AiSkillMatchCandidateDto> Matches { get; set; } = [];
}
