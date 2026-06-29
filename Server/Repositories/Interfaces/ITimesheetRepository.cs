using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface ITimesheetRepository
{
	Task<bool> ExistsForWeekAsync(long resourceProfileId, DateOnly weekStart, CancellationToken cancellationToken = default);
	Task<Timesheet?> GetByResourceProfileAndWeekAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
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
	Task<Timesheet> AddSubmittedAsync(Timesheet timesheet, CancellationToken cancellationToken = default);
	Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> GetRecentActivityTagsByResourceProfilesAsync(
		IReadOnlyList<long> resourceProfileIds,
		DateOnly sinceWeekStart,
		CancellationToken cancellationToken = default);
	Task<TimesheetComplianceTracking> GetOrCreateComplianceTrackingAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task ClearComplianceFreezeAsync(long resourceProfileId, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
