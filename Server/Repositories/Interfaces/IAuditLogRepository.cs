using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAuditLogRepository
{
	Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
	Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
		int page,
		int pageSize,
		long? actorUserId,
		string? actionType,
		string? entityName,
		long? entityId,
		DateTime? from,
		DateTime? to,
		CancellationToken cancellationToken = default);
}
