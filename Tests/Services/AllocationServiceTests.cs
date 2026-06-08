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
			new EmployeeRepository(_context),
			new ProjectRepository(_context));
	}

	[Fact]
	public async Task CreateAllocationAsync_WithOverlappingUtilization_ThrowsOverAllocation()
	{
		var (managerId, employeeId, projectId) = await SeedTeamAndProjectAsync(existingUtilization: 60);

		var act = () => _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = new DateOnly(2026, 3, 1),
			AllocationEndDate = new DateOnly(2026, 6, 30)
		}, managerId);

		await act.Should().ThrowAsync<OverAllocationException>();
	}

	[Fact]
	public async Task CreateAllocationAsync_WithValidInput_CreatesAllocation()
	{
		var (managerId, employeeId, projectId) = await SeedTeamAndProjectAsync(existingUtilization: 0);

		var result = await _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = new DateOnly(2026, 3, 1),
			AllocationEndDate = new DateOnly(2026, 6, 30)
		}, managerId);

		result.AllocationStatus.Should().Be("ACTIVE");
		result.EmploymentStatus.Should().Be("ALLOCATED");
	}

	[Fact]
	public async Task EndAllocationAsync_WhenLastAllocation_RevertsEmployeeToBench()
	{
		var (managerId, employeeId, projectId, allocationId) = await SeedActiveAllocationAsync();

		await _allocationService.EndAllocationAsync(allocationId, managerId);

		var employee = await _context.Employees.FindAsync(employeeId);
		var allocation = await _context.ProjectAllocations.FindAsync(allocationId);

		employee!.EmploymentStatus.Should().Be("BENCH");
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

		var employee = new Employee
		{
			UserId = employeeUser.Id,
			ManagerId = manager.Id,
			EmployeeCode = "EMP-000002",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Employees.Add(employee);

		var project = new Project
		{
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha Portal",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31),
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
				EmployeeId = employee.Id,
				ProjectId = project.Id,
				AllocationPercentage = existingUtilization,
				AllocationStartDate = new DateOnly(2026, 3, 1),
				AllocationEndDate = new DateOnly(2026, 6, 30),
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = manager.Id,
				CreatedAt = now,
				UpdatedAt = now
			});
			await _context.SaveChangesAsync();
		}

		return (manager.Id, employee.Id, project.Id);
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
