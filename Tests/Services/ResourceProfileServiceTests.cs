using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class ResourceProfileServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly ResourceProfileService _resourceProfileService;

	public ResourceProfileServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_resourceProfileService = new ResourceProfileService(
			_context,
			new ResourceProfileRepository(_context),
			new UserRepository(_context),
			new SkillRepository(_context),
			new AllocationRepository(_context));
	}

	[Fact]
	public async Task DeactivateEmployeeAsync_EndsActiveAllocationsAndBlocksLogin()
	{
		var (resourceProfile, user) = await SeedResourceProfileWithAllocationAsync();

		await _resourceProfileService.DeactivateEmployeeAsync(resourceProfile.Id, actorUserId: 1);

		var updatedResourceProfile = await _context.ResourceProfiles.FindAsync(resourceProfile.Id);
		var updatedUser = await _context.Users.FindAsync(user.Id);
		var allocation = await _context.ProjectAllocations.FindAsync(1L);

		updatedResourceProfile!.IsActive.Should().BeFalse();
		updatedResourceProfile.EmploymentStatus.Should().Be("BENCH");
		updatedUser!.IsActive.Should().BeFalse();
		allocation!.AllocationStatus.Should().Be("ENDED");
		allocation.AllocationEndDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

		var audit = await _context.AuditLogs.FirstOrDefaultAsync(item => item.EntityId == resourceProfile.Id);
		audit.Should().NotBeNull();
		audit!.ActionType.Should().Be("DEACTIVATE");
		audit.EntityName.Should().Be("RESOURCE_PROFILES");
	}

	[Fact]
	public async Task RestoreTimesheetAccessAsync_ForManagersTeamMember_ClearsFrozenFlag()
	{
		var (resourceProfile, _) = await SeedResourceProfileWithAllocationAsync();
		resourceProfile.IsTimesheetFrozen = true;
		resourceProfile.TimesheetFrozenAt = DateTime.UtcNow;
		_context.TimesheetComplianceTrackings.Add(new TimesheetComplianceTracking
		{
			ResourceProfileId = resourceProfile.Id,
			WeekStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
			ReminderCount = 2,
			IsFrozenForWeek = true
		});
		await _context.SaveChangesAsync();

		await _resourceProfileService.RestoreTimesheetAccessAsync(resourceProfile.Id, managerUserId: 1);

		var updatedResourceProfile = await _context.ResourceProfiles.FindAsync(resourceProfile.Id);
		updatedResourceProfile!.IsTimesheetFrozen.Should().BeFalse();
		updatedResourceProfile.TimesheetFrozenAt.Should().BeNull();
		(await _context.TimesheetComplianceTrackings.SingleAsync()).IsFrozenForWeek.Should().BeFalse();
		(await _context.AuditLogs.AnyAsync(log => log.ActionType == "RESTORE_TIMESHEET_ACCESS")).Should().BeTrue();
	}

	private async Task<(ResourceProfile resourceProfile, User user)> SeedResourceProfileWithAllocationAsync()
	{
		var now = DateTime.UtcNow;

		var manager = new User
		{
			Id = 1,
			Username = "manager",
			Email = "manager@techserve.com",
			FullName = "Manager",
			PasswordHash = "hash",
			Role = "MANAGER",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var user = new User
		{
			Id = 2,
			Username = "employee",
			Email = "employee@techserve.com",
			FullName = "Employee",
			PasswordHash = "hash",
			Role = "EMPLOYEE",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(manager, user);
		await _context.SaveChangesAsync();

		var resourceProfile = new ResourceProfile
		{
			Id = 10,
			UserId = user.Id,
			ManagerId = manager.Id,
			ResourceProfileCode = "EMP-000002",
			EmploymentStatus = "ALLOCATED",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.ResourceProfiles.Add(resourceProfile);

		var project = new Project
		{
			Id = 201,
			ProjectCode = "ALPHA",
			ProjectName = "Alpha Portal",
			ManagerUserId = manager.Id,
			StartDate = DateOnly.FromDateTime(now),
			EndDate = DateOnly.FromDateTime(now.AddMonths(6)),
			ProjectStatus = "ACTIVE",
			TotalStoryPoints = 100,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);

		_context.ProjectAllocations.Add(new ProjectAllocation
		{
			Id = 1,
			ResourceProfileId = resourceProfile.Id,
			ProjectId = project.Id,
			AllocationPercentage = 100,
			AllocationStartDate = DateOnly.FromDateTime(now.AddMonths(-1)),
			AllocationEndDate = DateOnly.FromDateTime(now.AddMonths(3)),
			AllocationStatus = "ACTIVE",
			AllocatedByManagerId = manager.Id,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync();
		return (resourceProfile, user);
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
