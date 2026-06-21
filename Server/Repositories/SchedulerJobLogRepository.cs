using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class SchedulerJobLogRepository : ISchedulerJobLogRepository
{
	private readonly PrmDbContext _context;

	public SchedulerJobLogRepository(PrmDbContext context)
	{
		_context = context;
	}

	public async Task LogAsync(
		string jobName,
		string status,
		DateTime startedAt,
		DateTime completedAt,
		string? errorMessage = null,
		CancellationToken cancellationToken = default)
	{
		_context.SchedulerJobLogs.Add(new SchedulerJobLog
		{
			JobName = jobName,
			Status = status,
			StartedAt = startedAt,
			CompletedAt = completedAt,
			ErrorMessage = errorMessage
		});

		await _context.SaveChangesAsync(cancellationToken);
	}
}
