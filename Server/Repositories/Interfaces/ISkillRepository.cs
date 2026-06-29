using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface ISkillRepository
{
	Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default);
	Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default);
	Task<ResourceProfileSkill?> GetResourceProfileSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default);
	Task AddResourceProfileSkillAsync(ResourceProfileSkill resourceProfileSkill, CancellationToken cancellationToken = default);
	Task RemoveResourceProfileSkillAsync(ResourceProfileSkill resourceProfileSkill, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ResourceProfileSkill>> GetResourceProfileSkillsAsync(long resourceProfileId, CancellationToken cancellationToken = default);
	Task<IReadOnlyDictionary<long, string>> GetNamesByIdsAsync(
		IReadOnlyList<long> skillIds,
		CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
