using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class SkillRepository : ISkillRepository
{
	private readonly PrmDbContext _context;

	public SkillRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default)
	{
		return _context.Skills
			.FirstOrDefaultAsync(skill => skill.SkillName == skillName, cancellationToken);
	}

	public async Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default)
	{
		_context.Skills.Add(skill);
		await _context.SaveChangesAsync(cancellationToken);
		return skill;
	}

	public Task<EmployeeSkill?> GetEmployeeSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default)
	{
		return _context.EmployeeSkills
			.FirstOrDefaultAsync(
				employeeSkill => employeeSkill.EmployeeId == employeeId && employeeSkill.SkillId == skillId,
				cancellationToken);
	}

	public async Task AddEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
	{
		_context.EmployeeSkills.Add(employeeSkill);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
	{
		_context.EmployeeSkills.Remove(employeeSkill);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<EmployeeSkill>> GetEmployeeSkillsAsync(long employeeId, CancellationToken cancellationToken = default)
	{
		return await _context.EmployeeSkills
			.Include(employeeSkill => employeeSkill.Skill)
			.Where(employeeSkill => employeeSkill.EmployeeId == employeeId)
			.OrderBy(employeeSkill => employeeSkill.Skill.SkillName)
			.ToListAsync(cancellationToken);
	}
}
