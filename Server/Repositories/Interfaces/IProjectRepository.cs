using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface IProjectRepository
{
	Task<Project?> GetByIdAsync(long projectId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);
	Task<ProjectMilestone?> GetMilestoneAsync(long projectId, long milestoneId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectMilestone>> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default);
	Task<ProjectMilestone> AddMilestoneAsync(ProjectMilestone milestone, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(long managerUserId, CancellationToken cancellationToken = default);
	Task<Project?> GetDetailByIdAsync(long projectId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Project>> GetActiveProjectsWithMilestonesAsync(CancellationToken cancellationToken = default);
	Task UpdateHealthStatusAsync(long projectId, string healthStatus, CancellationToken cancellationToken = default);
	Task UpdateLastRiskSummaryAsync(long projectId, string? summary, CancellationToken cancellationToken = default);
}
