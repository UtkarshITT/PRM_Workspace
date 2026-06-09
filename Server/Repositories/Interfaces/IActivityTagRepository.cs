using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IActivityTagRepository
{
	Task<IReadOnlyList<ActivityTag>> GetAllActiveAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ActivityTag>> GetByIdsAsync(IReadOnlyList<long> tagIds, CancellationToken cancellationToken = default);
}
