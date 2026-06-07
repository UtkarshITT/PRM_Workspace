using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IUserRepository
{
	Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
	Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
