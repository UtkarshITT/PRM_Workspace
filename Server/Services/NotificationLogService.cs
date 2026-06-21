using PRM.Server.Models.DTOs.Notifications;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface INotificationLogService
{
	Task<IReadOnlyList<NotificationLogDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}

public class NotificationLogService : INotificationLogService
{
	private readonly INotificationLogRepository _notificationLogRepository;

	public NotificationLogService(INotificationLogRepository notificationLogRepository)
	{
		_notificationLogRepository = notificationLogRepository;
	}

	public async Task<IReadOnlyList<NotificationLogDto>> GetRecentAsync(
		int take,
		CancellationToken cancellationToken = default)
	{
		var boundedTake = Math.Clamp(take, 1, 200);
		var logs = await _notificationLogRepository.GetRecentAsync(boundedTake, cancellationToken);

		return logs.Select(log => new NotificationLogDto
		{
			Id = log.Id,
			NotificationType = log.NotificationType,
			RecipientName = log.RecipientUser.FullName,
			RecipientEmail = log.RecipientEmail,
			Subject = log.Subject,
			Status = log.Status,
			DeliveryChannel = log.DeliveryChannel,
			RelatedEntityName = log.RelatedEntityName,
			RelatedEntityId = log.RelatedEntityId,
			WeekStartDate = log.WeekStartDate,
			ErrorMessage = log.ErrorMessage,
			CreatedAt = log.CreatedAt
		}).ToList();
	}
}
