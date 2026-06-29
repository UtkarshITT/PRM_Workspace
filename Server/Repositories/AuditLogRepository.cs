using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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

	public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
		int page,
		int pageSize,
		long? actorUserId,
		string? actionType,
		string? entityName,
		long? entityId,
		DateTime? from,
		DateTime? to,
		CancellationToken cancellationToken = default)
	{
		var query = _context.AuditLogs
			.Include(log => log.ActorUser)
			.AsQueryable();

		if (actorUserId.HasValue)
		{
			query = query.Where(log => log.ActorUserId == actorUserId.Value);
		}

		if (!string.IsNullOrWhiteSpace(actionType))
		{
			query = query.Where(log => log.ActionType == actionType);
		}

		if (!string.IsNullOrWhiteSpace(entityName))
		{
			query = query.Where(log => log.EntityName == entityName);
		}

		if (entityId.HasValue)
		{
			query = query.Where(log => log.EntityId == entityId.Value);
		}

		if (from.HasValue)
		{
			query = query.Where(log => log.CreatedAt >= from.Value);
		}

		if (to.HasValue)
		{
			query = query.Where(log => log.CreatedAt <= to.Value);
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.OrderByDescending(log => log.CreatedAt)
			.ThenByDescending(log => log.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return (items, totalCount);
	}
}
