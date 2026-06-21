using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface ITimesheetRepository
{
	Task<bool> ExistsForWeekAsync(long resourceProfileId, DateOnly weekStart, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Timesheet>> GetByResourceProfileIdAsync(long resourceProfileId, CancellationToken cancellationToken = default);
	Task<Timesheet?> GetDetailByIdForResourceProfileAsync(long timesheetId, long resourceProfileId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Timesheet>> GetByTeamAndWeekAsync(
		IReadOnlyList<long> teamResourceProfileIds,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task<Timesheet?> GetDetailByIdAsync(long timesheetId, CancellationToken cancellationToken = default);
	Task<decimal> GetLoggedHoursForProjectResourceProfileWeekAsync(
		long projectId,
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task<decimal> GetLoggedHoursForProjectWeekAsync(
		long projectId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task InsertMissedAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
}
