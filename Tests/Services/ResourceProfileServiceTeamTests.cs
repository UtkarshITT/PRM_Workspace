using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class ResourceProfileServiceTeamTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly ResourceProfileService _resourceProfileService;

	public ResourceProfileServiceTeamTests()
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
	public async Task GetTeamDashboardAsync_ReturnsOnlyManagerTeam()
	{
		var managerId = await SeedManagersAndResourceProfilesAsync();

		var dashboard = await _resourceProfileService.GetTeamDashboardAsync(managerId);

		dashboard.BenchMembers.Should().HaveCount(1);
		dashboard.ActiveMembers.Should().HaveCount(1);
		dashboard.BenchMembers[0].FullName.Should().Be("Bench Employee");
		dashboard.ActiveMembers[0].FullName.Should().Be("Active Employee");
	}

	private async Task<long> SeedManagersAndResourceProfilesAsync()
	{
		var now = DateTime.UtcNow;

		var manager1 = new User
		{
			Username = "mgr1",
			Email = "mgr1@techserve.com",
			FullName = "Manager One",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var manager2 = new User
		{
			Username = "mgr2",
			Email = "mgr2@techserve.com",
			FullName = "Manager Two",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(manager1, manager2);
		await _context.SaveChangesAsync();

		var benchUser = new User
		{
			Username = "bench",
			Email = "bench@techserve.com",
			FullName = "Bench Employee",
			PasswordHash = "hash",
			Role = Roles.Employee,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var activeUser = new User
		{
			Username = "active",
			Email = "active@techserve.com",
			FullName = "Active Employee",
			PasswordHash = "hash",
			Role = Roles.Employee,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var otherTeamUser = new User
		{
			Username = "other",
			Email = "other@techserve.com",
			FullName = "Other Team",
			PasswordHash = "hash",
			Role = Roles.Employee,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(benchUser, activeUser, otherTeamUser);
		await _context.SaveChangesAsync();

		var benchResourceProfile = new ResourceProfile
		{
			UserId = benchUser.Id,
			ManagerId = manager1.Id,
			ResourceProfileCode = "EMP-000001",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var activeResourceProfile = new ResourceProfile
		{
			UserId = activeUser.Id,
			ManagerId = manager1.Id,
			ResourceProfileCode = "EMP-000002",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var otherResourceProfile = new ResourceProfile
		{
			UserId = otherTeamUser.Id,
			ManagerId = manager2.Id,
			ResourceProfileCode = "EMP-000003",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.ResourceProfiles.AddRange(benchResourceProfile, activeResourceProfile, otherResourceProfile);

		var project = new Project
		{
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31),
			ProjectStatus = ProjectStatuses.Active,
			ManagerUserId = manager1.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		_context.ProjectAllocations.Add(new ProjectAllocation
		{
			ResourceProfileId = activeResourceProfile.Id,
			ProjectId = project.Id,
			AllocationPercentage = 75,
			AllocationStartDate = new DateOnly(2026, 1, 1),
			AllocationEndDate = new DateOnly(2026, 12, 31),
			AllocationStatus = "ACTIVE",
			AllocatedByManagerId = manager1.Id,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync();
		return manager1.Id;
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
