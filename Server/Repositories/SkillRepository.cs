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

	public Task<ResourceProfileSkill?> GetResourceProfileSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default)
	{
		return _context.ResourceProfileSkills
			.FirstOrDefaultAsync(
				resourceProfileSkill => resourceProfileSkill.ResourceProfileId == resourceProfileId && resourceProfileSkill.SkillId == skillId,
				cancellationToken);
	}

	public async Task AddResourceProfileSkillAsync(ResourceProfileSkill resourceProfileSkill, CancellationToken cancellationToken = default)
	{
		_context.ResourceProfileSkills.Add(resourceProfileSkill);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveResourceProfileSkillAsync(ResourceProfileSkill resourceProfileSkill, CancellationToken cancellationToken = default)
	{
		_context.ResourceProfileSkills.Remove(resourceProfileSkill);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ResourceProfileSkill>> GetResourceProfileSkillsAsync(long resourceProfileId, CancellationToken cancellationToken = default)
	{
		return await _context.ResourceProfileSkills
			.Include(resourceProfileSkill => resourceProfileSkill.Skill)
			.Where(resourceProfileSkill => resourceProfileSkill.ResourceProfileId == resourceProfileId)
			.OrderBy(resourceProfileSkill => resourceProfileSkill.Skill.SkillName)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyDictionary<long, string>> GetNamesByIdsAsync(
		IReadOnlyList<long> skillIds,
		CancellationToken cancellationToken = default)
	{
		if (skillIds.Count == 0)
		{
			return new Dictionary<long, string>();
		}

		return await _context.Skills
			.Where(skill => skillIds.Contains(skill.Id))
			.ToDictionaryAsync(skill => skill.Id, skill => skill.SkillName, cancellationToken);
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
