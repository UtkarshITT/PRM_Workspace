using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
	private readonly PrmDbContext _context;

	public AuditLogRepository(PrmDbContext context)
	{
		_context = context;
	}

	public async Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
	{
		_context.AuditLogs.Add(auditLog);
		await _context.SaveChangesAsync(cancellationToken);
	}
}
