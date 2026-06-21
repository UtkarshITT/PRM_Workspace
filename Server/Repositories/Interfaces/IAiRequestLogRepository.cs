using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IAiRequestLogRepository
{
	Task LogAsync(AiRequestLog log, CancellationToken cancellationToken = default);
	Task<int> CountByUserSinceAsync(long userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
