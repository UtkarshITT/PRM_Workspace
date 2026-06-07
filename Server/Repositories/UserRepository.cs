using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class UserRepository : IUserRepository
{
	private readonly PrmDbContext _context;

	public UserRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
	{
		return _context.Users
			.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
	}

	public Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return _context.Users
			.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
