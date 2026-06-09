using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class ActivityTagRepository : IActivityTagRepository
{
	private readonly PrmDbContext _context;

	public ActivityTagRepository(PrmDbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<ActivityTag>> GetAllActiveAsync(CancellationToken cancellationToken = default)
	{
		return await _context.ActivityTags
			.Where(tag => tag.IsActive)
			.OrderBy(tag => tag.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ActivityTag>> GetByIdsAsync(
		IReadOnlyList<long> tagIds,
		CancellationToken cancellationToken = default)
	{
		if (tagIds.Count == 0)
		{
			return [];
		}

		return await _context.ActivityTags
			.Where(tag => tagIds.Contains(tag.Id) && tag.IsActive)
			.ToListAsync(cancellationToken);
	}
}
