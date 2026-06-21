using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Allocations;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class AllocationServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly AllocationService _allocationService;

	public AllocationServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_allocationService = new AllocationService(
			_context,
			new AllocationRepository(_context),
			new ResourceProfileRepository(_context),
			new ProjectRepository(_context));
	}

	[Fact]
	public async Task CreateAllocationAsync_WithOverlappingUtilization_ThrowsOverAllocation()
	{
		var (managerId, employeeId, projectId) = await SeedTeamAndProjectAsync(existingUtilization: 60);
		var startDate = DateOnly.FromDateTime(DateTime.Today);

		var act = () => _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = startDate,
			AllocationEndDate = startDate.AddMonths(3)
		}, managerId);

		await act.Should().ThrowAsync<OverAllocationException>();
	}

	[Fact]
	public async Task CreateAllocationAsync_WithValidInput_CreatesAllocation()
	{
		var (managerId, employeeId, projectId) = await SeedTeamAndProjectAsync(existingUtilization: 0);
		var startDate = DateOnly.FromDateTime(DateTime.Today);

		var result = await _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = startDate,
			AllocationEndDate = startDate.AddMonths(3)
		}, managerId);

		result.AllocationStatus.Should().Be("ACTIVE");
		result.EmploymentStatus.Should().Be("ALLOCATED");
	}

	[Fact]
	public async Task EndAllocationAsync_WhenLastAllocation_RevertsEmployeeToBench()
	{
		var (managerId, employeeId, projectId, allocationId) = await SeedActiveAllocationAsync();

		await _allocationService.EndAllocationAsync(allocationId, managerId);

		var resourceProfile = await _context.ResourceProfiles.FindAsync(employeeId);
		var allocation = await _context.ProjectAllocations.FindAsync(allocationId);

		resourceProfile!.EmploymentStatus.Should().Be("BENCH");
		allocation!.AllocationStatus.Should().Be("ENDED");
	}

	[Fact]
	public async Task EndAllocationAsync_WhenNotProjectManager_ThrowsValidation()
	{
		var (_, _, _, allocationId) = await SeedActiveAllocationAsync();
		var otherManagerId = 999;

		var act = () => _allocationService.EndAllocationAsync(allocationId, otherManagerId);
		await act.Should().ThrowAsync<ValidationException>();
	}

	private async Task<(long managerId, long employeeId, long projectId)> SeedTeamAndProjectAsync(decimal existingUtilization)
	{
		var now = DateTime.UtcNow;
		var manager = new User
		{
			Username = "manager",
			Email = "manager@techserve.com",
			FullName = "Manager",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		var employeeUser = new User
		{
			Username = "employee",
			Email = "employee@techserve.com",
			FullName = "Employee",
			PasswordHash = "hash",
			Role = Roles.Employee,
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
			ResourceProfileCode = "EMP-000002",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.ResourceProfiles.Add(resourceProfile);

		var startDate = DateOnly.FromDateTime(DateTime.Today);
		var project = new Project
		{
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha Portal",
			StartDate = startDate,
			EndDate = startDate.AddMonths(6),
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 100,
			ManagerUserId = manager.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		if (existingUtilization > 0)
		{
			_context.ProjectAllocations.Add(new ProjectAllocation
			{
				ResourceProfileId = resourceProfile.Id,
				ProjectId = project.Id,
				AllocationPercentage = existingUtilization,
				AllocationStartDate = startDate,
				AllocationEndDate = startDate.AddMonths(3),
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = manager.Id,
				CreatedAt = now,
				UpdatedAt = now
			});
			await _context.SaveChangesAsync();
		}

		return (manager.Id, resourceProfile.Id, project.Id);
	}

	private async Task<(long managerId, long employeeId, long projectId, long allocationId)> SeedActiveAllocationAsync()
	{
		var (managerId, employeeId, projectId) = await SeedTeamAndProjectAsync(existingUtilization: 50);
		var allocation = await _context.ProjectAllocations.FirstAsync();
		return (managerId, employeeId, projectId, allocation.Id);
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
