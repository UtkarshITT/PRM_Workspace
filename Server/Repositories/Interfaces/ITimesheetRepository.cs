using PRM.Server.Models.Entities;

namespace PRM.Server.Repositories.Interfaces;

public interface ITimesheetRepository
{
	Task<bool> ExistsForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Timesheet>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
	Task<Timesheet?> GetDetailByIdForEmployeeAsync(long timesheetId, long employeeId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Timesheet>> GetByTeamAndWeekAsync(
		IReadOnlyList<long> teamEmployeeIds,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task<Timesheet?> GetDetailByIdAsync(long timesheetId, CancellationToken cancellationToken = default);
	Task<decimal> GetLoggedHoursForProjectEmployeeWeekAsync(
		long projectId,
		long employeeId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
}
