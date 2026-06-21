using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface INotificationLogRepository
{
	Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
	Task<bool> ExistsAsync(
		string notificationType,
		string relatedEntityName,
		long relatedEntityId,
		DateOnly? weekStartDate,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
