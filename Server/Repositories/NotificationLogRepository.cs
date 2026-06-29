using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PRM.Server.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
	private readonly PrmDbContext _context;

	public NotificationLogRepository(PrmDbContext context)
	{
		_context = context;
	}

	public async Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
	{
		_context.NotificationLogs.Add(log);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public Task<bool> ExistsAsync(
		string notificationType,
		string relatedEntityName,
		long relatedEntityId,
		DateOnly? weekStartDate,
		CancellationToken cancellationToken = default)
	{
		return _context.NotificationLogs.AnyAsync(
			log => log.NotificationType == notificationType
				&& log.RelatedEntityName == relatedEntityName
				&& log.RelatedEntityId == relatedEntityId
				&& log.WeekStartDate == weekStartDate,
			cancellationToken);
	}

	public async Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
	{
		return await _context.NotificationLogs
			.Include(log => log.RecipientUser)
			.OrderByDescending(log => log.CreatedAt)
			.Take(take)
			.ToListAsync(cancellationToken);
	}
}
