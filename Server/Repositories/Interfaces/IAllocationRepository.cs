using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAllocationRepository
{
	Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
}
