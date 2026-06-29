namespace PRM.Server.Models.DTOs.Notifications;

public class NotificationLogDto
{
	public long Id { get; set; }
	public string NotificationType { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string RecipientEmail { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string DeliveryChannel { get; set; } = string.Empty;
	public string? RelatedEntityName { get; set; }
	public long? RelatedEntityId { get; set; }
	public DateOnly? WeekStartDate { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime CreatedAt { get; set; }
}
