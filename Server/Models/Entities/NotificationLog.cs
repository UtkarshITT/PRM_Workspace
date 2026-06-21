namespace PRM.Server.Models.Entities;

public class NotificationLog
{
	public long Id { get; set; }
	public string NotificationType { get; set; } = string.Empty;
	public long RecipientUserId { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
	public string Status { get; set; } = "PENDING";
	public string DeliveryChannel { get; set; } = "CONSOLE";
	public string? RelatedEntityName { get; set; }
	public long? RelatedEntityId { get; set; }
	public DateOnly? WeekStartDate { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime? SentAt { get; set; }
	public DateTime CreatedAt { get; set; }

	public User RecipientUser { get; set; } = null!;
}
