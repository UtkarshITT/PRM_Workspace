namespace PRM.Server.Models.DTOs.Ai;

public class AiSkillMatchResponseDto
{
	public string? Message { get; set; }
	public string? Summary { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = "AI-generated. Verify before confirming.";
	public List<string> GapAnalysis { get; set; } = [];
	public List<AiSkillMatchCandidateDto> Candidates { get; set; } = [];
}

public class AiSkillMatchCandidateDto
{
	public long EmployeeId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public int Rank { get; set; }
	public int MatchScore { get; set; }
	public string Reason { get; set; } = string.Empty;
	public decimal AvailabilityPercent { get; set; }
}
