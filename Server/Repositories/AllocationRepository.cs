using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class AllocationRepository : IAllocationRepository
{
	private readonly PrmDbContext _context;

	public AllocationRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<ProjectAllocation?> GetByIdWithDetailsAsync(long allocationId, CancellationToken cancellationToken = default)
	{
		return _context.ProjectAllocations
			.Include(allocation => allocation.Employee)
			.ThenInclude(employee => employee.User)
			.Include(allocation => allocation.Project)
			.FirstOrDefaultAsync(allocation => allocation.Id == allocationId, cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectAllocations
			.Where(allocation => allocation.EmployeeId == employeeId && allocation.AllocationStatus == "ACTIVE")
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default)
	{
		var query = _context.ProjectAllocations
			.Include(allocation => allocation.Employee)
			.ThenInclude(employee => employee.User)
			.Include(allocation => allocation.Project)
			.AsQueryable();

		if (employeeId.HasValue)
		{
			query = query.Where(allocation => allocation.EmployeeId == employeeId.Value);
		}

		if (projectId.HasValue)
		{
			query = query.Where(allocation => allocation.ProjectId == projectId.Value);
		}

		if (!string.IsNullOrWhiteSpace(status))
		{
			query = query.Where(allocation => allocation.AllocationStatus == status);
		}

		return await query
			.OrderBy(allocation => allocation.Employee.User.FullName)
			.ThenBy(allocation => allocation.Project.ProjectName)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetByEmployeeIdWithProjectsAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectAllocations
			.Include(allocation => allocation.Project)
			.Where(allocation => allocation.EmployeeId == employeeId)
			.OrderByDescending(allocation => allocation.AllocationStatus == "ACTIVE")
			.ThenBy(allocation => allocation.Project.ProjectName)
			.ToListAsync(cancellationToken);
	}
}
