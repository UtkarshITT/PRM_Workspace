using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
	private readonly PrmDbContext _context;

	public TimesheetRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<bool> ExistsForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default)
	{
		return _context.Timesheets.AnyAsync(
			timesheet => timesheet.EmployeeId == employeeId && timesheet.WeekStartDate == weekStart,
			cancellationToken);
	}

	public async Task<IReadOnlyList<Timesheet>> GetByEmployeeIdAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		return await _context.Timesheets
			.Where(timesheet => timesheet.EmployeeId == employeeId)
			.OrderByDescending(timesheet => timesheet.WeekStartDate)
			.ToListAsync(cancellationToken);
	}

	public Task<Timesheet?> GetDetailByIdForEmployeeAsync(
		long timesheetId,
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		return _context.Timesheets
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.ActivityTags)
			.ThenInclude(tag => tag.ActivityTag)
			.FirstOrDefaultAsync(
				timesheet => timesheet.Id == timesheetId && timesheet.EmployeeId == employeeId,
				cancellationToken);
	}

	public async Task<IReadOnlyList<Timesheet>> GetByTeamAndWeekAsync(
		IReadOnlyList<long> teamEmployeeIds,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		if (teamEmployeeIds.Count == 0)
		{
			return [];
		}

		return await _context.Timesheets
			.Include(timesheet => timesheet.Employee)
			.ThenInclude(employee => employee.User)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Where(timesheet => teamEmployeeIds.Contains(timesheet.EmployeeId) && timesheet.WeekStartDate == weekStart)
			.OrderBy(timesheet => timesheet.Employee.User.FullName)
			.ToListAsync(cancellationToken);
	}

	public Task<Timesheet?> GetDetailByIdAsync(long timesheetId, CancellationToken cancellationToken = default)
	{
		return _context.Timesheets
			.Include(timesheet => timesheet.Employee)
			.ThenInclude(employee => employee.User)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.ActivityTags)
			.ThenInclude(tag => tag.ActivityTag)
			.FirstOrDefaultAsync(timesheet => timesheet.Id == timesheetId, cancellationToken);
	}

	public async Task<decimal> GetLoggedHoursForProjectEmployeeWeekAsync(
		long projectId,
		long employeeId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		return await _context.TimesheetLineItems
			.Where(lineItem =>
				lineItem.ProjectId == projectId
				&& lineItem.Timesheet.EmployeeId == employeeId
				&& lineItem.Timesheet.WeekStartDate == weekStart
				&& lineItem.Timesheet.Status == "SUBMITTED")
			.SumAsync(lineItem => lineItem.HoursLogged, cancellationToken);
	}
}
