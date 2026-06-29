using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class AiRequestLogRepository : IAiRequestLogRepository
{
	private readonly PrmDbContext _context;

	public AiRequestLogRepository(PrmDbContext context)
	{
		_context = context;
	}

	public async Task LogAsync(AiRequestLog log, CancellationToken cancellationToken = default)
	{
		_context.AiRequestLogs.Add(log);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public Task<int> CountByUserSinceAsync(long userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
	{
		return _context.AiRequestLogs
			.CountAsync(log => log.RequestedByUserId == userId && log.CreatedAt >= sinceUtc, cancellationToken);
	}
}
