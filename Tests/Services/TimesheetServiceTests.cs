using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Helpers;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Timesheets;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class TimesheetServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly TimesheetService _timesheetService;
	private static readonly DateOnly TestWeek = new(2026, 5, 4);

	public TimesheetServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_timesheetService = new TimesheetService(
			new TimesheetRepository(_context),
			new AllocationRepository(_context),
			new ActivityTagRepository(_context),
			new SystemConfigRepository(_context),
			new ResourceProfileRepository(_context),
			new ProjectRepository(_context),
			new AuditService(new AuditLogRepository(_context)));
	}

	[Fact]
	public async Task SubmitTimesheetAsync_WithValidInput_SubmitsSuccessfully()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync();

		var result = await _timesheetService.SubmitTimesheetAsync(employeeId, CreateValidDto(projectId, tagIds));

		result.Status.Should().Be("SUBMITTED");
		result.TotalHours.Should().Be(20);
		(await _context.Timesheets.CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task SubmitTimesheetAsync_DuplicateWeek_ThrowsDuplicateTimesheetException()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync();
		await _timesheetService.SubmitTimesheetAsync(employeeId, CreateValidDto(projectId, tagIds));

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, CreateValidDto(projectId, tagIds));

		await act.Should().ThrowAsync<DuplicateTimesheetException>();
	}

	[Fact]
	public async Task SubmitTimesheetAsync_FutureWeek_ThrowsFutureWeekException()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync();
		var futureWeek = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);
		while (futureWeek.DayOfWeek != DayOfWeek.Monday)
		{
			futureWeek = futureWeek.AddDays(1);
		}

		var dto = CreateValidDto(projectId, tagIds);
		dto.WeekStartDate = futureWeek;

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, dto);

		await act.Should().ThrowAsync<FutureWeekException>();
	}

	[Fact]
	public async Task SubmitTimesheetAsync_HoursExceedAllocation_ThrowsValidationException()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync(allocationPercent: 50);

		var dto = CreateValidDto(projectId, tagIds, hours: 25);

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, dto);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*allocation cap*");
	}

	[Fact]
	public async Task SubmitTimesheetAsync_UnallocatedProject_ThrowsValidationException()
	{
		var (employeeId, _, tagIds) = await SeedEmployeeWithAllocationAsync();

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, CreateValidDto(9999, tagIds));

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*not an active allocation*");
	}

	[Fact]
	public async Task MarkMissedTimesheetsAsync_CreatesMissed_WhenNoSubmission()
	{
		var employeeId = await SeedEmployeeWithAllocationForLastWeekAsync();

		var created = await _timesheetService.MarkMissedTimesheetsAsync();

		created.Should().Be(1);
		var timesheet = await _context.Timesheets.FirstAsync(timesheet => timesheet.ResourceProfileId == employeeId);
		timesheet.Status.Should().Be("MISSED");
		timesheet.TotalHours.Should().Be(0);
	}

	[Fact]
	public async Task MarkMissedTimesheetsAsync_IsIdempotent()
	{
		var employeeId = await SeedEmployeeWithAllocationForLastWeekAsync();

		await _timesheetService.MarkMissedTimesheetsAsync();
		var secondRun = await _timesheetService.MarkMissedTimesheetsAsync();

		secondRun.Should().Be(0);
		(await _context.Timesheets.CountAsync(timesheet => timesheet.ResourceProfileId == employeeId)).Should().Be(1);
	}

	[Fact]
	public async Task GetRemindersAsync_WithLastWeekAllocationAndNoSubmission_ReturnsReminder()
	{
		var employeeId = await SeedEmployeeWithAllocationForLastWeekAsync();

		var reminders = await _timesheetService.GetRemindersAsync(employeeId);

		reminders.Messages.Should().ContainSingle(message => message.Contains("has not been submitted"));
	}

	[Fact]
	public async Task GetRemindersAsync_WithBenchEmployee_ReturnsNoPendingReminder()
	{
		var employeeId = await SeedBenchEmployeeAsync();

		var reminders = await _timesheetService.GetRemindersAsync(employeeId);

		reminders.Messages.Should().BeEmpty();
	}

	[Fact]
	public async Task SubmitTimesheetAsync_TotalHoursExceedMax_ThrowsValidationException()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync(allocationPercent: 100);
		var secondProjectId = await SeedSecondProjectAsync(employeeId);
		var tagId = tagIds[0];

		var dto = new SubmitTimesheetDto
		{
			WeekStartDate = TestWeek,
			LineItems =
			[
				new TimesheetLineItemDto
				{
					ProjectId = projectId,
					HoursLogged = 25,
					ActivityTagIds = [tagId]
				},
				new TimesheetLineItemDto
				{
					ProjectId = secondProjectId,
					HoursLogged = 20,
					ActivityTagIds = [tagId]
				}
			]
		};

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, dto);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*maximum weekly hours*");
	}

	[Fact]
	public async Task SubmitTimesheetAsync_WhenTimesheetAccessFrozen_ThrowsValidationException()
	{
		var (employeeId, projectId, tagIds) = await SeedEmployeeWithAllocationAsync();
		var resourceProfile = await _context.ResourceProfiles.FindAsync(employeeId);
		resourceProfile!.IsTimesheetFrozen = true;
		await _context.SaveChangesAsync();

		var act = () => _timesheetService.SubmitTimesheetAsync(employeeId, CreateValidDto(projectId, tagIds));

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*frozen*");
	}

	private static SubmitTimesheetDto CreateValidDto(
		long projectId,
		IReadOnlyList<long> tagIds,
		decimal hours = 20) =>
		new()
		{
			WeekStartDate = TestWeek,
			LineItems =
			[
				new TimesheetLineItemDto
				{
					ProjectId = projectId,
					HoursLogged = hours,
					ActivityTagIds = tagIds.ToList()
				}
			]
		};

	private async Task<long> SeedEmployeeWithAllocationForLastWeekAsync()
	{
		var lastWeekStart = WeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
		var (employeeId, _, _) = await SeedEmployeeWithAllocationAsync(allocationWeek: lastWeekStart);
		return employeeId;
	}

	private async Task<long> SeedBenchEmployeeAsync()
	{
		var now = DateTime.UtcNow;
		var manager = new User
		{
			Username = "mgr",
			Email = "mgr@test.com",
			FullName = "Manager",
			Role = Roles.Manager,
			PasswordHash = "hash",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var employeeUser = new User
		{
			Username = "bench.emp",
			Email = "bench.emp@test.com",
			FullName = "Bench Employee",
			Role = Roles.Employee,
			PasswordHash = "hash",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(manager, employeeUser);
		await _context.SaveChangesAsync();

		var resourceProfile = new ResourceProfile
		{
			UserId = employeeUser.Id,
			ManagerId = manager.Id,
			ResourceProfileCode = "EMP-000001",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.ResourceProfiles.Add(resourceProfile);
		await _context.SaveChangesAsync();

		return resourceProfile.Id;
	}

	private async Task<(long EmployeeId, long ProjectId, IReadOnlyList<long> TagIds)> SeedEmployeeWithAllocationAsync(
		decimal allocationPercent = 100,
		DateOnly? allocationWeek = null)
	{
		var now = DateTime.UtcNow;
		var manager = new User
		{
			Username = "mgr",
			Email = "mgr@test.com",
			FullName = "Manager",
			Role = Roles.Manager,
			PasswordHash = "hash",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var employeeUser = new User
		{
			Username = "emp",
			Email = "emp@test.com",
			FullName = "Employee",
			Role = Roles.Employee,
			PasswordHash = "hash",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(manager, employeeUser);
		await _context.SaveChangesAsync();

		var resourceProfile = new ResourceProfile
		{
			UserId = employeeUser.Id,
			ManagerId = manager.Id,
			ResourceProfileCode = "EMP-000001",
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
			TotalStoryPoints = 100,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31),
			ManagerUserId = manager.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		var devTag = new ActivityTag
		{
			TagCode = "DEV",
			TagName = "Development",
			IsActive = true,
			CreatedAt = now
		};
		var otherTag = new ActivityTag
		{
			TagCode = "OTHER",
			TagName = "Other",
			IsActive = true,
			CreatedAt = now
		};
		_context.ActivityTags.AddRange(devTag, otherTag);
		_context.SystemConfigurations.Add(new SystemConfiguration
		{
			ConfigKey = SystemConfigKeys.MaxWeeklyHours,
			ConfigValue = "40",
			UpdatedAt = now
		});

		var effectiveAllocationWeek = allocationWeek ?? TestWeek;
		_context.ProjectAllocations.Add(new ProjectAllocation
		{
			ResourceProfileId = resourceProfile.Id,
			ProjectId = project.Id,
			AllocationPercentage = allocationPercent,
			AllocationStartDate = effectiveAllocationWeek.AddMonths(-1),
			AllocationEndDate = effectiveAllocationWeek.AddMonths(2),
			AllocationStatus = "ACTIVE",
			AllocatedByManagerId = manager.Id,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync();
		return (resourceProfile.Id, project.Id, new List<long> { devTag.Id });
	}

	private async Task<long> SeedSecondProjectAsync(long employeeId)
	{
		var managerId = await _context.Users.Where(user => user.Role == Roles.Manager).Select(user => user.Id).FirstAsync();
		var now = DateTime.UtcNow;

		var project = new Project
		{
			ProjectCode = "PRJ-000002",
			ProjectName = "Beta",
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 100,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31),
			ManagerUserId = managerId,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		_context.ProjectAllocations.Add(new ProjectAllocation
		{
			ResourceProfileId = employeeId,
			ProjectId = project.Id,
			AllocationPercentage = 100,
			AllocationStartDate = TestWeek.AddMonths(-1),
			AllocationEndDate = TestWeek.AddMonths(2),
			AllocationStatus = "ACTIVE",
			AllocatedByManagerId = managerId,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync();
		return project.Id;
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
