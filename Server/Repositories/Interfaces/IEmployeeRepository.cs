using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IEmployeeRepository
{
	Task<Employee?> GetByIdAsync(long employeeId, CancellationToken cancellationToken = default);
	Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Employee>> GetAllAsync(string? status, string? department, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Employee>> GetByManagerIdAsync(long managerUserId, CancellationToken cancellationToken = default);
	Task<Employee?> GetTeamMemberAsync(long employeeId, long managerUserId, CancellationToken cancellationToken = default);
	Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
