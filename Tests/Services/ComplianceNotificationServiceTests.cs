using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Helpers;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Email;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class ComplianceNotificationServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly ComplianceNotificationService _service;

	public ComplianceNotificationServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		var notificationLogRepository = new NotificationLogRepository(_context);
		var emailService = new EmailNotificationService(
			new SystemConfigRepository(_context, new ConfigurationBuilder().Build()),
			notificationLogRepository,
			NullLogger<EmailNotificationService>.Instance);
		_service = new ComplianceNotificationService(_context, emailService, notificationLogRepository);
	}

	[Fact]
	public async Task ProcessTimesheetComplianceAsync_SendsTwoRemindersThenFreezes()
	{
		var resourceProfileId = await SeedAllocatedResourceProfileWithMissedTimesheetAsync();

		var firstRun = await _service.ProcessTimesheetComplianceAsync();
		var secondRun = await _service.ProcessTimesheetComplianceAsync();
		var thirdRun = await _service.ProcessTimesheetComplianceAsync();

		firstRun.RemindersSent.Should().Be(1);
		secondRun.RemindersSent.Should().Be(1);
		thirdRun.EmployeesFrozen.Should().Be(1);

		var resourceProfile = await _context.ResourceProfiles.FindAsync(resourceProfileId);
		resourceProfile!.IsTimesheetFrozen.Should().BeTrue();
		(await _context.NotificationLogs.CountAsync(log => log.NotificationType == "TIMESHEET_REMINDER")).Should().Be(2);
		(await _context.NotificationLogs.CountAsync(log => log.NotificationType == "TIMESHEET_FREEZE")).Should().Be(2);
	}

	[Fact]
	public async Task SendProjectAtRiskNotificationsAsync_SendsOnlyOncePerProjectPerWeek()
	{
		await SeedEmailConfigAsync();
		await SeedAtRiskProjectAsync();

		var firstRun = await _service.SendProjectAtRiskNotificationsAsync();
		var secondRun = await _service.SendProjectAtRiskNotificationsAsync();

		firstRun.Should().Be(1);
		secondRun.Should().Be(0);
		(await _context.NotificationLogs.CountAsync(log => log.NotificationType == "PROJECT_AT_RISK")).Should().Be(1);
	}

	private async Task<long> SeedAllocatedResourceProfileWithMissedTimesheetAsync()
	{
		await SeedEmailConfigAsync();
		var now = DateTime.UtcNow;
		var weekStart = WeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(now));
		var manager = CreateUser(1, "manager", "MANAGER");
		var employee = CreateUser(2, "employee", "EMPLOYEE");
		_context.Users.AddRange(manager, employee);
		await _context.SaveChangesAsync();

		var resourceProfile = new ResourceProfile
		{
			UserId = employee.Id,
			ManagerId = manager.Id,
			ResourceProfileCode = "EMP-000002",
			EmploymentStatus = "ALLOCATED",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.ResourceProfiles.Add(resourceProfile);
		await _context.SaveChangesAsync();

		var project = new Project
		{
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha",
			ProjectStatus = ProjectStatuses.Active,
			HealthStatus = "GREEN",
			StartDate = weekStart.AddDays(-30),
			EndDate = weekStart.AddDays(30),
			ManagerUserId = manager.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		_context.ProjectAllocations.Add(new ProjectAllocation
		{
			ResourceProfileId = resourceProfile.Id,
			ProjectId = project.Id,
			AllocatedByManagerId = manager.Id,
			AllocationPercentage = 100,
			AllocationStartDate = weekStart,
			AllocationEndDate = weekStart.AddDays(6),
			AllocationStatus = "ACTIVE",
			CreatedAt = now,
			UpdatedAt = now
		});
		_context.Timesheets.Add(new Timesheet
		{
			ResourceProfileId = resourceProfile.Id,
			WeekStartDate = weekStart,
			Status = "MISSED",
			CreatedAt = now,
			UpdatedAt = now
		});
		await _context.SaveChangesAsync();
		return resourceProfile.Id;
	}

	private async Task SeedAtRiskProjectAsync()
	{
		var now = DateTime.UtcNow;
		var manager = CreateUser(10, "risk.manager", "MANAGER");
		_context.Users.Add(manager);
		await _context.SaveChangesAsync();
		_context.Projects.Add(new Project
		{
			ProjectCode = "PRJ-000010",
			ProjectName = "Risky",
			ProjectStatus = ProjectStatuses.Active,
			HealthStatus = "RED",
			LastRiskSummary = "Delayed milestone",
			StartDate = DateOnly.FromDateTime(now).AddDays(-30),
			EndDate = DateOnly.FromDateTime(now).AddDays(30),
			ManagerUserId = manager.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		});
		await _context.SaveChangesAsync();
	}

	private async Task SeedEmailConfigAsync()
	{
		var now = DateTime.UtcNow;
		if (await _context.SystemConfigurations.AnyAsync())
		{
			return;
		}

		_context.SystemConfigurations.AddRange(
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailConsoleEnabled, ConfigValue = "true", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailSmtpEnabled, ConfigValue = "false", UpdatedAt = now });
		await _context.SaveChangesAsync();
	}

	private static User CreateUser(long id, string username, string role) =>
		new()
		{
			Id = id,
			Username = username,
			Email = $"{username}@test.com",
			FullName = username,
			PasswordHash = "hash",
			Role = role,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

	public void Dispose()
	{
		_context.Dispose();
	}
}
