using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAuditLogRepository
{
	Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
