namespace PRM.Server.Repositories.Interfaces;

public interface ISchedulerJobLogRepository
{
	Task LogAsync(
		string jobName,
		string status,
		DateTime startedAt,
		DateTime completedAt,
		string? errorMessage = null,
		CancellationToken cancellationToken = default);
}
