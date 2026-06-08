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
			.FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
	}

	public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
	{
		return _context.Users
			.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
	}

	public Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return _context.Users
			.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
	}

	public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Users
			.OrderBy(user => user.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
	{
		_context.Users.Add(user);
		await _context.SaveChangesAsync(cancellationToken);
		return user;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
