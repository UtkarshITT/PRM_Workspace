using System.Net;
using System.Net.Mail;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Email;

public class EmailNotificationService : IEmailNotificationService
{
	public const string ChannelConsole = "CONSOLE";
	public const string ChannelSmtp = "SMTP";

	private readonly ISystemConfigRepository _systemConfigRepository;
	private readonly INotificationLogRepository _notificationLogRepository;
	private readonly ILogger<EmailNotificationService> _logger;

	public EmailNotificationService(
		ISystemConfigRepository systemConfigRepository,
		INotificationLogRepository notificationLogRepository,
		ILogger<EmailNotificationService> logger)
	{
		_systemConfigRepository = systemConfigRepository;
		_notificationLogRepository = notificationLogRepository;
		_logger = logger;
	}

	public async Task SendAsync(NotificationEmailRequest request, CancellationToken cancellationToken = default)
	{
		var settings = await _systemConfigRepository.GetEmailSettingsAsync(cancellationToken);
		var anyChannelAttempted = false;

		if (settings.ConsoleEnabled)
		{
			anyChannelAttempted = true;
			await SendViaConsoleAsync(request, cancellationToken);
		}

		if (settings.SmtpEnabled)
		{
			if (settings.IsSmtpConfigured)
			{
				anyChannelAttempted = true;
				await SendViaSmtpAsync(request, settings, cancellationToken);
			}
			else
			{
				await LogSkippedAsync(
					request,
					ChannelSmtp,
					"SMTP is enabled but Email:Smtp host or from address is not configured.",
					cancellationToken);
			}
		}

		if (!anyChannelAttempted)
		{
			_logger.LogWarning(
				"Notification {NotificationType} for user {RecipientUserId} was not sent — all channels are disabled.",
				request.NotificationType,
				request.RecipientUserId);
		}
	}

	private async Task SendViaConsoleAsync(NotificationEmailRequest request, CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation(
				"""
				[EMAIL:CONSOLE] To: {RecipientEmail} | Subject: {Subject}
				{Body}
				""",
				request.RecipientEmail,
				request.Subject,
				request.Body);

			await LogSuccessAsync(request, ChannelConsole, cancellationToken);
		}
		catch (Exception ex)
		{
			await LogFailureAsync(request, ChannelConsole, ex.Message, cancellationToken);
		}
	}

	private async Task SendViaSmtpAsync(
		NotificationEmailRequest request,
		EmailSettings settings,
		CancellationToken cancellationToken)
	{
		try
		{
			using var message = new MailMessage
			{
				From = new MailAddress(settings.SmtpFromAddress),
				Subject = request.Subject,
				Body = request.Body,
				IsBodyHtml = false
			};
			message.To.Add(request.RecipientEmail);

			using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
			{
				EnableSsl = settings.SmtpPort is 465 or 587,
				Credentials = string.IsNullOrWhiteSpace(settings.SmtpUsername)
					? CredentialCache.DefaultNetworkCredentials
					: new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
			};

			await client.SendMailAsync(message, cancellationToken);
			await LogSuccessAsync(request, ChannelSmtp, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "SMTP delivery failed for {RecipientEmail}", request.RecipientEmail);
			await LogFailureAsync(request, ChannelSmtp, ex.Message, cancellationToken);
		}
	}

	private Task LogSuccessAsync(
		NotificationEmailRequest request,
		string channel,
		CancellationToken cancellationToken)
	{
		return _notificationLogRepository.AddAsync(new NotificationLog
		{
			NotificationType = request.NotificationType,
			RecipientUserId = request.RecipientUserId,
			RecipientEmail = request.RecipientEmail,
			Subject = request.Subject,
			Body = request.Body,
			Status = "SENT",
			DeliveryChannel = channel,
			RelatedEntityName = request.RelatedEntityName,
			RelatedEntityId = request.RelatedEntityId,
			WeekStartDate = request.WeekStartDate,
			SentAt = DateTime.UtcNow,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);
	}

	private Task LogFailureAsync(
		NotificationEmailRequest request,
		string channel,
		string errorMessage,
		CancellationToken cancellationToken)
	{
		return _notificationLogRepository.AddAsync(new NotificationLog
		{
			NotificationType = request.NotificationType,
			RecipientUserId = request.RecipientUserId,
			RecipientEmail = request.RecipientEmail,
			Subject = request.Subject,
			Body = request.Body,
			Status = "FAILED",
			DeliveryChannel = channel,
			RelatedEntityName = request.RelatedEntityName,
			RelatedEntityId = request.RelatedEntityId,
			WeekStartDate = request.WeekStartDate,
			ErrorMessage = errorMessage,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);
	}

	private Task LogSkippedAsync(
		NotificationEmailRequest request,
		string channel,
		string reason,
		CancellationToken cancellationToken)
	{
		return _notificationLogRepository.AddAsync(new NotificationLog
		{
			NotificationType = request.NotificationType,
			RecipientUserId = request.RecipientUserId,
			RecipientEmail = request.RecipientEmail,
			Subject = request.Subject,
			Body = request.Body,
			Status = "SKIPPED",
			DeliveryChannel = channel,
			RelatedEntityName = request.RelatedEntityName,
			RelatedEntityId = request.RelatedEntityId,
			WeekStartDate = request.WeekStartDate,
			ErrorMessage = reason,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);
	}
}
