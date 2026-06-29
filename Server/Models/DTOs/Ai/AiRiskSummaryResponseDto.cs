namespace PRM.Server.Models.DTOs.Ai;

public class AiRiskSummaryResponseDto
{
	public string? Message { get; set; }
	public string? Paragraph { get; set; }
	public bool AiGenerated { get; set; }
	public string Disclaimer { get; set; } = "AI-generated from milestone and timesheet data. Verify before acting.";
	public string? ProjectName { get; set; }
	public string? HealthStatus { get; set; }
}
