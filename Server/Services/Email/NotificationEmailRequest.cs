namespace PRM.Server.Services.Email;

public class NotificationEmailRequest
{
	public string NotificationType { get; set; } = string.Empty;
	public long RecipientUserId { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
	public string? RelatedEntityName { get; set; }
	public long? RelatedEntityId { get; set; }
	public DateOnly? WeekStartDate { get; set; }
}
