using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Helpers;
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
			.Include(allocation => allocation.ResourceProfile)
			.ThenInclude(resourceProfile => resourceProfile.User)
			.Include(allocation => allocation.Project)
			.FirstOrDefaultAsync(allocation => allocation.Id == allocationId, cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByResourceProfileIdAsync(
		long resourceProfileId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectAllocations
			.Where(allocation => allocation.ResourceProfileId == resourceProfileId && allocation.AllocationStatus == "ACTIVE")
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default)
	{
		var query = _context.ProjectAllocations
			.Include(allocation => allocation.ResourceProfile)
			.ThenInclude(resourceProfile => resourceProfile.User)
			.Include(allocation => allocation.Project)
			.AsQueryable();

		if (employeeId.HasValue)
		{
			query = query.Where(allocation => allocation.ResourceProfileId == employeeId.Value);
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
			.OrderBy(allocation => allocation.ResourceProfile.User.FullName)
			.ThenBy(allocation => allocation.Project.ProjectName)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetByResourceProfileIdWithProjectsAsync(
		long resourceProfileId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectAllocations
			.Include(allocation => allocation.Project)
			.Where(allocation => allocation.ResourceProfileId == resourceProfileId)
			.OrderByDescending(allocation => allocation.AllocationStatus == "ACTIVE")
			.ThenBy(allocation => allocation.Project.ProjectName)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectAllocation>> GetActiveAllocationsForWeekAsync(
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken = default)
	{
		var allocations = await _context.ProjectAllocations
			.Where(allocation => allocation.AllocationStatus == "ACTIVE")
			.ToListAsync(cancellationToken);

		return allocations
			.Where(allocation => UtilizationCalculator.PeriodsOverlap(
				allocation.AllocationStartDate,
				allocation.AllocationEndDate,
				weekStart,
				weekEnd))
			.ToList();
	}

	public Task AddAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default)
	{
		return _context.ProjectAllocations.AddAsync(allocation, cancellationToken).AsTask();
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
