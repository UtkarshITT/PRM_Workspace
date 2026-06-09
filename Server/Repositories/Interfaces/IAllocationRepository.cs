using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAllocationRepository
{
	Task<ProjectAllocation?> GetByIdWithDetailsAsync(long allocationId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectAllocation>> GetByEmployeeIdWithProjectsAsync(
		long employeeId,
		CancellationToken cancellationToken = default);
}
