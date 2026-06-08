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

	public Task<Employee?> GetByIdAsync(long employeeId, CancellationToken cancellationToken = default)
	{
		return _context.Employees
			.Include(employee => employee.User)
			.Include(employee => employee.Manager)
			.Include(employee => employee.EmployeeSkills)
			.ThenInclude(employeeSkill => employeeSkill.Skill)
			.FirstOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);
	}

	public Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return _context.Employees
			.FirstOrDefaultAsync(employee => employee.UserId == userId, cancellationToken);
	}

	public async Task<IReadOnlyList<Employee>> GetAllAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default)
	{
		var query = _context.Employees
			.Include(employee => employee.User)
			.Include(employee => employee.Manager)
			.Include(employee => employee.EmployeeSkills)
			.ThenInclude(employeeSkill => employeeSkill.Skill)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(status))
		{
			query = query.Where(employee => employee.EmploymentStatus == status);
		}

		if (!string.IsNullOrWhiteSpace(department))
		{
			query = query.Where(employee => employee.Department == department);
		}

		return await query
			.OrderBy(employee => employee.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
	{
		_context.Employees.Add(employee);
		await _context.SaveChangesAsync(cancellationToken);
		return employee;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
