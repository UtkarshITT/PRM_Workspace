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

	public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectAllocations
			.Where(allocation => allocation.EmployeeId == employeeId && allocation.AllocationStatus == "ACTIVE")
			.ToListAsync(cancellationToken);
	}
}
