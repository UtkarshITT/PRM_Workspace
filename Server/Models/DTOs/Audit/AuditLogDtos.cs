namespace PRM.Server.Models.DTOs.Audit;

public class AuditLogItemDto
{
	public long Id { get; set; }
	public long ActorUserId { get; set; }
	public string ActorUsername { get; set; } = string.Empty;
	public string ActorName { get; set; } = string.Empty;
	public string ActionType { get; set; } = string.Empty;
	public string EntityName { get; set; } = string.Empty;
	public long EntityId { get; set; }
	public string? Notes { get; set; }
	public string? OldValues { get; set; }
	public string? NewValues { get; set; }
	public string? CorrelationId { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class AuditLogPageDto
{
	public IReadOnlyList<AuditLogItemDto> Items { get; set; } = [];
	public int Page { get; set; }
	public int PageSize { get; set; }
	public int TotalCount { get; set; }
	public int TotalPages { get; set; }
}
