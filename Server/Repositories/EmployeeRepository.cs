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

	public async Task<IReadOnlyList<Employee>> GetByManagerIdAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		return await _context.Employees
			.Include(employee => employee.User)
			.Include(employee => employee.EmployeeSkills)
			.ThenInclude(employeeSkill => employeeSkill.Skill)
			.Include(employee => employee.ProjectAllocations)
			.Where(employee => employee.ManagerId == managerUserId && employee.IsActive)
			.OrderBy(employee => employee.Id)
			.ToListAsync(cancellationToken);
	}

	public Task<Employee?> GetTeamMemberAsync(
		long employeeId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		return _context.Employees
			.Include(employee => employee.User)
			.Include(employee => employee.EmployeeSkills)
			.ThenInclude(employeeSkill => employeeSkill.Skill)
			.Include(employee => employee.ProjectAllocations)
			.ThenInclude(allocation => allocation.Project)
			.FirstOrDefaultAsync(
				employee => employee.Id == employeeId && employee.ManagerId == managerUserId && employee.IsActive,
				cancellationToken);
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
