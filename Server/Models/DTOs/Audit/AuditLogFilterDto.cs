namespace PRM.Server.Models.DTOs.Audit;

public class AuditLogFilterDto
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public long? ActorUserId { get; set; }
	public string? ActionType { get; set; }
	public string? EntityName { get; set; }
	public long? EntityId { get; set; }
	public DateTime? From { get; set; }
	public DateTime? To { get; set; }
}
