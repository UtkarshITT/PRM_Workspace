using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Scheduler;

public class BackgroundScheduler : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<BackgroundScheduler> _logger;

	public BackgroundScheduler(IServiceProvider serviceProvider, ILogger<BackgroundScheduler> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Background scheduler started.");

		while (!stoppingToken.IsCancellationRequested)
		{
			var startedAt = DateTime.UtcNow;

			try
			{
				using var scope = _serviceProvider.CreateScope();
				var configRepository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
				var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
				var timesheetService = scope.ServiceProvider.GetRequiredService<ITimesheetService>();
				var complianceNotificationService = scope.ServiceProvider.GetRequiredService<IComplianceNotificationService>();
				var jobLogRepository = scope.ServiceProvider.GetRequiredService<ISchedulerJobLogRepository>();

				await projectService.EvaluateAllProjectsHealthAsync(stoppingToken);
				var missedCreated = await timesheetService.MarkMissedTimesheetsAsync(stoppingToken);
				var complianceResult = await complianceNotificationService.ProcessTimesheetComplianceAsync(stoppingToken);
				var projectRiskNotifications = await complianceNotificationService.SendProjectAtRiskNotificationsAsync(stoppingToken);

				var completedAt = DateTime.UtcNow;
				await jobLogRepository.LogAsync(
					"BackgroundScheduler",
					"SUCCESS",
					startedAt,
					completedAt,
					cancellationToken: stoppingToken);

				_logger.LogInformation(
					"Scheduler completed in {ElapsedMs}ms. Missed timesheets created: {MissedTimesheetsCreated}. Reminders sent: {RemindersSent}. Employees frozen: {EmployeesFrozen}. Project risk notifications: {ProjectRiskNotifications}",
					(completedAt - startedAt).TotalMilliseconds,
					missedCreated,
					complianceResult.RemindersSent,
					complianceResult.EmployeesFrozen,
					projectRiskNotifications);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Scheduler run failed");

				try
				{
					using var scope = _serviceProvider.CreateScope();
					var jobLogRepository = scope.ServiceProvider.GetRequiredService<ISchedulerJobLogRepository>();
					await jobLogRepository.LogAsync(
						"BackgroundScheduler",
						"FAILED",
						startedAt,
						DateTime.UtcNow,
						ex.Message,
						stoppingToken);
				}
				catch (Exception logEx)
				{
					_logger.LogError(logEx, "Failed to write scheduler job log");
				}
			}

			try
			{
				using var scope = _serviceProvider.CreateScope();
				var configRepository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
				var intervalHours = await configRepository.GetSchedulerIntervalHoursAsync(stoppingToken);
				await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
		}

		_logger.LogInformation("Background scheduler stopped.");
	}
}
