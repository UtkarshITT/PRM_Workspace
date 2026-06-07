using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IEmployeeRepository
{
	Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
