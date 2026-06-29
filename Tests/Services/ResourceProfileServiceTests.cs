using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Employees;
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
			new ResourceProfileRepository(_context),
			new UserRepository(_context),
			new SkillRepository(_context),
			new AllocationRepository(_context),
			new TimesheetRepository(_context),
			new AuditService(new AuditLogRepository(_context)));
	}

	[Fact]
	public async Task GetAllEmployeesAsync_ExcludesAdminAndManagerResourceProfiles()
	{
		var now = DateTime.UtcNow;
		var admin = CreateUser(1, "admin", Roles.Admin, now);
		var manager = CreateUser(2, "manager", Roles.Manager, now);
		var employee = CreateUser(3, "employee", Roles.Employee, now);

		_context.Users.AddRange(admin, manager, employee);
		await _context.SaveChangesAsync();

		_context.ResourceProfiles.AddRange(
			CreateResourceProfile(10, admin.Id, "IT", now),
			CreateResourceProfile(11, manager.Id, "Delivery", now),
			CreateResourceProfile(12, employee.Id, "Backend", now));
		await _context.SaveChangesAsync();

		var result = await _resourceProfileService.GetAllEmployeesAsync(null, null);

		result.Should().ContainSingle();
		result[0].Id.Should().Be(12);
		result[0].FullName.Should().Be("employee");
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

	[Fact]
	public async Task RestoreTimesheetAccessAsync_ForNonTeamMember_ThrowsNotFoundException()
	{
		var (resourceProfile, _) = await SeedResourceProfileWithAllocationAsync();
		resourceProfile.IsTimesheetFrozen = true;
		resourceProfile.ManagerId = 999;
		await _context.SaveChangesAsync();

		var act = () => _resourceProfileService.RestoreTimesheetAccessAsync(resourceProfile.Id, managerUserId: 1);

		await act.Should().ThrowAsync<NotFoundException>()
			.WithMessage("*not found on your team*");
	}

	[Fact]
	public async Task RestoreTimesheetAccessAsync_WhenNotFrozen_ThrowsValidationException()
	{
		var (resourceProfile, _) = await SeedResourceProfileWithAllocationAsync();

		var act = () => _resourceProfileService.RestoreTimesheetAccessAsync(resourceProfile.Id, managerUserId: 1);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*not frozen*");
	}

	[Fact]
	public async Task UpdateSkillProficiencyAsync_WithAssignedSkill_UpdatesLevel()
	{
		var (resourceProfile, _) = await SeedResourceProfileWithAllocationAsync();
		var skill = await SeedResourceProfileSkillAsync(resourceProfile.Id, ProficiencyLevels.Beginner);

		var result = await _resourceProfileService.UpdateSkillProficiencyAsync(
			resourceProfile.Id,
			skill.Id,
			new UpdateSkillProficiencyDto { ProficiencyLevel = ProficiencyLevels.Advanced },
			actorUserId: 1);

		var updatedSkill = await _context.ResourceProfileSkills.FindAsync(resourceProfile.Id, skill.Id);
		updatedSkill!.ProficiencyLevel.Should().Be(ProficiencyLevels.Advanced);
		result.Should().ContainSingle(item =>
			item.SkillId == skill.Id && item.ProficiencyLevel == ProficiencyLevels.Advanced);
	}

	[Fact]
	public async Task UpdateSkillProficiencyAsync_WithUnassignedSkill_ThrowsNotFoundException()
	{
		var (resourceProfile, _) = await SeedResourceProfileWithAllocationAsync();

		var act = () => _resourceProfileService.UpdateSkillProficiencyAsync(
			resourceProfile.Id,
			skillId: 999,
			new UpdateSkillProficiencyDto { ProficiencyLevel = ProficiencyLevels.Intermediate },
			actorUserId: 1);

		await act.Should().ThrowAsync<NotFoundException>()
			.WithMessage("*not assigned*");
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

	private async Task<Skill> SeedResourceProfileSkillAsync(long resourceProfileId, string proficiencyLevel)
	{
		var now = DateTime.UtcNow;
		var skill = new Skill
		{
			SkillName = "C#",
			Category = SkillCategories.Backend,
			IsActive = true,
			CreatedAt = now
		};

		_context.Skills.Add(skill);
		await _context.SaveChangesAsync();

		_context.ResourceProfileSkills.Add(new ResourceProfileSkill
		{
			ResourceProfileId = resourceProfileId,
			SkillId = skill.Id,
			ProficiencyLevel = proficiencyLevel,
			CreatedAt = now
		});
		await _context.SaveChangesAsync();

		return skill;
	}

	private static User CreateUser(long id, string username, string role, DateTime now)
	{
		return new User
		{
			Id = id,
			Username = username,
			Email = $"{username}@techserve.com",
			FullName = username,
			PasswordHash = "hash",
			Role = role,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
	}

	private static ResourceProfile CreateResourceProfile(long id, long userId, string department, DateTime now)
	{
		return new ResourceProfile
		{
			Id = id,
			UserId = userId,
			ResourceProfileCode = $"EMP-{id:D6}",
			Department = department,
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
