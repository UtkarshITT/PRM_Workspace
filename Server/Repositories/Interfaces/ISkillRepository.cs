using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface ISkillRepository
{
	Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default);
	Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default);
	Task<EmployeeSkill?> GetEmployeeSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default);
	Task AddEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
	Task RemoveEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<EmployeeSkill>> GetEmployeeSkillsAsync(long employeeId, CancellationToken cancellationToken = default);
}
