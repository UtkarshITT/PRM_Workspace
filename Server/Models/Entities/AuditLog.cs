namespace PRM.Server.Models.Entities;

public class AuditLog
{
	public long Id { get; set; }
	public long ActorUserId { get; set; }
	public string EntityName { get; set; } = string.Empty;
	public long EntityId { get; set; }
	public string ActionType { get; set; } = string.Empty;
	public string? OldValues { get; set; }
	public string? NewValues { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? CorrelationId { get; set; }

	public User ActorUser { get; set; } = null!;
}
