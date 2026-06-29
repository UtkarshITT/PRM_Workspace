namespace PRM.Server.Services.Email;

/// <summary>
/// Sends notifications through all enabled delivery channels simultaneously
/// (console/Serilog and SMTP are independent — both can be active at once).
/// </summary>
public interface IEmailNotificationService
{
	Task SendAsync(NotificationEmailRequest request, CancellationToken cancellationToken = default);
}
