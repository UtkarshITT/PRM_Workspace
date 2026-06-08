using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class EmployeeServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly EmployeeService _employeeService;

	public EmployeeServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_employeeService = new EmployeeService(
			_context,
			new EmployeeRepository(_context),
			new UserRepository(_context),
			new SkillRepository(_context),
			new AllocationRepository(_context));
	}

	[Fact]
	public async Task DeactivateEmployeeAsync_EndsActiveAllocationsAndBlocksLogin()
	{
		var (employee, user) = await SeedEmployeeWithAllocationAsync();

		await _employeeService.DeactivateEmployeeAsync(employee.Id, actorUserId: 1);

		var updatedEmployee = await _context.Employees.FindAsync(employee.Id);
		var updatedUser = await _context.Users.FindAsync(user.Id);
		var allocation = await _context.ProjectAllocations.FindAsync(1L);

		updatedEmployee!.IsActive.Should().BeFalse();
		updatedEmployee.EmploymentStatus.Should().Be("BENCH");
		updatedUser!.IsActive.Should().BeFalse();
		allocation!.AllocationStatus.Should().Be("ENDED");
		allocation.AllocationEndDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

		var audit = await _context.AuditLogs.FirstOrDefaultAsync(item => item.EntityId == employee.Id);
		audit.Should().NotBeNull();
		audit!.ActionType.Should().Be("DEACTIVATE");
	}

	private async Task<(Employee employee, User user)> SeedEmployeeWithAllocationAsync()
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

		var employee = new Employee
		{
			Id = 10,
			UserId = user.Id,
			ManagerId = manager.Id,
			EmployeeCode = "EMP-000002",
			EmploymentStatus = "ALLOCATED",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Employees.Add(employee);

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
			EmployeeId = employee.Id,
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
		return (employee, user);
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
