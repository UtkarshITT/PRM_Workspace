using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
	private readonly PrmDbContext _context;

	public EmployeeRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return _context.Employees
			.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
	}
}
