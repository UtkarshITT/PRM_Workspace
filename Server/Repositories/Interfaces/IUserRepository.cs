using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IUserRepository
{
	Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
	Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
	Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
