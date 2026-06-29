using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IResourceProfileRepository
{
	Task<ResourceProfile?> GetByIdAsync(long resourceProfileId, CancellationToken cancellationToken = default);
	Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ResourceProfile>> GetAllAsync(string? status, string? department, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ResourceProfile>> GetActiveOrganizationCandidatesAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ResourceProfile>> GetAllocatedForWeekAsync(
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ResourceProfile>> GetByManagerIdAsync(long managerUserId, CancellationToken cancellationToken = default);
	Task<ResourceProfile?> GetTeamMemberAsync(long resourceProfileId, long managerUserId, CancellationToken cancellationToken = default);
	Task<ResourceProfile> AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
