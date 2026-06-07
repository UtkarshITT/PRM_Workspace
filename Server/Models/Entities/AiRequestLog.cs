namespace PRM.Server.Models.Entities;

public class AiRequestLog
{
	public long Id { get; set; }
	public string RequestType { get; set; } = string.Empty;
	public string Prompt { get; set; } = string.Empty;
	public string? ResponseSummary { get; set; }
	public long RequestedByUserId { get; set; }
	public DateTime CreatedAt { get; set; }

	public User RequestedByUser { get; set; } = null!;
}
