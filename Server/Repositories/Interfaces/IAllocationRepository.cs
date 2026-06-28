using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAllocationRepository
{
	Task<ProjectAllocation?> GetByIdWithDetailsAsync(long allocationId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetActiveByResourceProfileIdAsync(long resourceProfileId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetByResourceProfileIdWithProjectsAsync(
		long resourceProfileId,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetActiveAllocationsForWeekAsync(
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken = default);
	Task AddAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
