using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
	private readonly PrmDbContext _context;
	private readonly IHttpContextAccessor? _httpContextAccessor;

	public AuditLogRepository(PrmDbContext context, IHttpContextAccessor? httpContextAccessor = null)
	{
		_context = context;
		_httpContextAccessor = httpContextAccessor;
	}

	public async Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
	{
		auditLog.CorrelationId ??= _httpContextAccessor?.HttpContext?.Items[global::PRM.Server.Middleware.CorrelationIdMiddleware.ItemName]?.ToString();
		_context.AuditLogs.Add(auditLog);
		await _context.SaveChangesAsync(cancellationToken);
	}
}
