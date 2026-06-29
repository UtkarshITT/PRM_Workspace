using PRM.Server.Constants;
using PRM.Server.Helpers;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Email;

namespace PRM.Server.Services.Interfaces;

public interface IComplianceNotificationService
{
	Task<ComplianceRunResult> ProcessTimesheetComplianceAsync(CancellationToken cancellationToken = default);
	Task<int> SendProjectAtRiskNotificationsAsync(CancellationToken cancellationToken = default);
}

public record ComplianceRunResult(int RemindersSent, int EmployeesFrozen);

public class ComplianceNotificationService : IComplianceNotificationService
{
	private const string TimesheetReminder = "TIMESHEET_REMINDER";
	private const string TimesheetFreeze = "TIMESHEET_FREEZE";
	private const string ProjectAtRisk = "PROJECT_AT_RISK";
	private const short MaxRemindersBeforeFreeze = 2;

	private readonly IEmailNotificationService _emailNotificationService;
	private readonly INotificationLogRepository _notificationLogRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly ITimesheetRepository _timesheetRepository;
	private readonly IProjectRepository _projectRepository;

	public ComplianceNotificationService(
		IEmailNotificationService emailNotificationService,
		INotificationLogRepository notificationLogRepository,
		IResourceProfileRepository resourceProfileRepository,
		ITimesheetRepository timesheetRepository,
		IProjectRepository projectRepository)
	{
		_emailNotificationService = emailNotificationService;
		_notificationLogRepository = notificationLogRepository;
		_resourceProfileRepository = resourceProfileRepository;
		_timesheetRepository = timesheetRepository;
		_projectRepository = projectRepository;
	}

	public async Task<ComplianceRunResult> ProcessTimesheetComplianceAsync(
		CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var weekStart = WeekHelper.GetLastCompletedWeekStart(today);
		var weekEnd = WeekHelper.GetWeekEnd(weekStart);
		var resourceProfiles = await GetAllocatedResourceProfilesForWeekAsync(weekStart, weekEnd, cancellationToken);
		var remindersSent = 0;
		var employeesFrozen = 0;

		foreach (var resourceProfile in resourceProfiles)
		{
			var timesheet = await _timesheetRepository.GetByResourceProfileAndWeekAsync(
				resourceProfile.Id,
				weekStart,
				cancellationToken);

			if (timesheet?.Status == "SUBMITTED")
			{
				continue;
			}

			var tracking = await GetOrCreateTrackingAsync(resourceProfile.Id, weekStart, cancellationToken);
			if (tracking.IsFrozenForWeek)
			{
				continue;
			}

			if (tracking.ReminderCount < MaxRemindersBeforeFreeze)
			{
				tracking.ReminderCount++;
				tracking.LastReminderAt = DateTime.UtcNow;
				await _timesheetRepository.SaveChangesAsync(cancellationToken);

				await SendTimesheetReminderAsync(resourceProfile, weekStart, tracking.ReminderCount, cancellationToken);
				remindersSent++;
				continue;
			}

			await FreezeTimesheetAccessAsync(resourceProfile, tracking, weekStart, cancellationToken);
			employeesFrozen++;
		}

		return new ComplianceRunResult(remindersSent, employeesFrozen);
	}

	public async Task<int> SendProjectAtRiskNotificationsAsync(CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var currentWeekStart = WeekHelper.GetWeekStart(today);
		var projects = await _projectRepository.GetActiveAtRiskProjectsAsync(cancellationToken);
		var sent = 0;

		foreach (var project in projects)
		{
			var alreadySent = await _notificationLogRepository.ExistsAsync(
				ProjectAtRisk,
				"PROJECTS",
				project.Id,
				currentWeekStart,
				cancellationToken);

			if (alreadySent)
			{
				continue;
			}

			await _emailNotificationService.SendAsync(new NotificationEmailRequest
			{
				NotificationType = ProjectAtRisk,
				RecipientUserId = project.ManagerUserId,
				RecipientEmail = project.ManagerUser.Email,
				Subject = $"Project at risk: {project.ProjectName} ({project.HealthStatus})",
				Body = BuildProjectRiskBody(project),
				RelatedEntityName = "PROJECTS",
				RelatedEntityId = project.Id,
				WeekStartDate = currentWeekStart
			}, cancellationToken);
			sent++;
		}

		return sent;
	}

	private async Task<IReadOnlyList<ResourceProfile>> GetAllocatedResourceProfilesForWeekAsync(
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken)
	{
		return await _resourceProfileRepository.GetAllocatedForWeekAsync(weekStart, weekEnd, cancellationToken);
	}

	private async Task<TimesheetComplianceTracking> GetOrCreateTrackingAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken)
	{
		return await _timesheetRepository.GetOrCreateComplianceTrackingAsync(resourceProfileId, weekStart, cancellationToken);
	}

	private Task SendTimesheetReminderAsync(
		ResourceProfile resourceProfile,
		DateOnly weekStart,
		short reminderCount,
		CancellationToken cancellationToken)
	{
		return _emailNotificationService.SendAsync(new NotificationEmailRequest
		{
			NotificationType = TimesheetReminder,
			RecipientUserId = resourceProfile.UserId,
			RecipientEmail = resourceProfile.User.Email,
			Subject = $"Timesheet reminder {reminderCount}: week {weekStart:dd-MMM-yyyy}",
			Body =
				$"Hello {resourceProfile.User.FullName},\n\nYour timesheet for week {weekStart:dd-MMM-yyyy} is still pending. Please submit it as soon as possible.",
			RelatedEntityName = "RESOURCE_PROFILES",
			RelatedEntityId = resourceProfile.Id,
			WeekStartDate = weekStart
		}, cancellationToken);
	}

	private async Task FreezeTimesheetAccessAsync(
		ResourceProfile resourceProfile,
		TimesheetComplianceTracking tracking,
		DateOnly weekStart,
		CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;
		resourceProfile.IsTimesheetFrozen = true;
		resourceProfile.TimesheetFrozenAt = now;
		resourceProfile.UpdatedAt = now;
		tracking.IsFrozenForWeek = true;
		await _timesheetRepository.SaveChangesAsync(cancellationToken);

		await _emailNotificationService.SendAsync(new NotificationEmailRequest
		{
			NotificationType = TimesheetFreeze,
			RecipientUserId = resourceProfile.UserId,
			RecipientEmail = resourceProfile.User.Email,
			Subject = "Timesheet access frozen",
			Body =
				$"Hello {resourceProfile.User.FullName},\n\nYour timesheet access has been frozen because the timesheet for week {weekStart:dd-MMM-yyyy} was not submitted after two reminders. Contact your manager to restore access.",
			RelatedEntityName = "RESOURCE_PROFILES",
			RelatedEntityId = resourceProfile.Id,
			WeekStartDate = weekStart
		}, cancellationToken);

		if (resourceProfile.Manager != null)
		{
			await _emailNotificationService.SendAsync(new NotificationEmailRequest
			{
				NotificationType = TimesheetFreeze,
				RecipientUserId = resourceProfile.Manager.Id,
				RecipientEmail = resourceProfile.Manager.Email,
				Subject = $"Timesheet access frozen: {resourceProfile.User.FullName}",
				Body =
					$"{resourceProfile.User.FullName}'s timesheet access has been frozen for missing week {weekStart:dd-MMM-yyyy}. Use restore-timesheet-access after follow-up.",
				RelatedEntityName = "RESOURCE_PROFILES",
				RelatedEntityId = resourceProfile.Id,
				WeekStartDate = weekStart
			}, cancellationToken);
		}
	}

	private static string BuildProjectRiskBody(Project project)
	{
		var riskSummary = string.IsNullOrWhiteSpace(project.LastRiskSummary)
			? "No AI risk summary has been cached yet. Review milestones, allocation, and timesheet data in My Projects."
			: project.LastRiskSummary;

		return
			$"Project: {project.ProjectName}\nHealth: {project.HealthStatus}\nEnd Date: {project.EndDate:dd-MMM-yyyy}\n\nRisk Summary:\n{riskSummary}\n\nSuggested action: review overdue milestones, recent logged hours, and team capacity.";
	}
}
