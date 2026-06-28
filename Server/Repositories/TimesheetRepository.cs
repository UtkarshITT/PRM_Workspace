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

	public Task<bool> ExistsForWeekAsync(long resourceProfileId, DateOnly weekStart, CancellationToken cancellationToken = default)
	{
		return _context.Timesheets.AnyAsync(
			timesheet => timesheet.ResourceProfileId == resourceProfileId && timesheet.WeekStartDate == weekStart,
			cancellationToken);
	}

	public Task<Timesheet?> GetByResourceProfileAndWeekAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		return _context.Timesheets.FirstOrDefaultAsync(
			timesheet => timesheet.ResourceProfileId == resourceProfileId && timesheet.WeekStartDate == weekStart,
			cancellationToken);
	}

	public async Task<IReadOnlyList<Timesheet>> GetByResourceProfileIdAsync(
		long resourceProfileId,
		CancellationToken cancellationToken = default)
	{
		return await _context.Timesheets
			.Where(timesheet => timesheet.ResourceProfileId == resourceProfileId)
			.OrderByDescending(timesheet => timesheet.WeekStartDate)
			.ToListAsync(cancellationToken);
	}

	public Task<Timesheet?> GetDetailByIdForResourceProfileAsync(
		long timesheetId,
		long resourceProfileId,
		CancellationToken cancellationToken = default)
	{
		return _context.Timesheets
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.ActivityTags)
			.ThenInclude(tag => tag.ActivityTag)
			.FirstOrDefaultAsync(
				timesheet => timesheet.Id == timesheetId && timesheet.ResourceProfileId == resourceProfileId,
				cancellationToken);
	}

	public async Task<IReadOnlyList<Timesheet>> GetByTeamAndWeekAsync(
		IReadOnlyList<long> teamResourceProfileIds,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		if (teamResourceProfileIds.Count == 0)
		{
			return [];
		}

		return await _context.Timesheets
			.Include(timesheet => timesheet.ResourceProfile)
			.ThenInclude(resourceProfile => resourceProfile.User)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Where(timesheet => teamResourceProfileIds.Contains(timesheet.ResourceProfileId) && timesheet.WeekStartDate == weekStart)
			.OrderBy(timesheet => timesheet.ResourceProfile.User.FullName)
			.ToListAsync(cancellationToken);
	}

	public Task<Timesheet?> GetDetailByIdAsync(long timesheetId, CancellationToken cancellationToken = default)
	{
		return _context.Timesheets
			.Include(timesheet => timesheet.ResourceProfile)
			.ThenInclude(resourceProfile => resourceProfile.User)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.Project)
			.Include(timesheet => timesheet.LineItems)
			.ThenInclude(lineItem => lineItem.ActivityTags)
			.ThenInclude(tag => tag.ActivityTag)
			.FirstOrDefaultAsync(timesheet => timesheet.Id == timesheetId, cancellationToken);
	}

	public async Task<decimal> GetLoggedHoursForProjectResourceProfileWeekAsync(
		long projectId,
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		return await _context.TimesheetLineItems
			.Where(lineItem =>
				lineItem.ProjectId == projectId
				&& lineItem.Timesheet.ResourceProfileId == resourceProfileId
				&& lineItem.Timesheet.WeekStartDate == weekStart
				&& lineItem.Timesheet.Status == "SUBMITTED")
			.SumAsync(lineItem => lineItem.HoursLogged, cancellationToken);
	}

	public async Task<decimal> GetLoggedHoursForProjectWeekAsync(
		long projectId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		return await _context.TimesheetLineItems
			.Where(lineItem =>
				lineItem.ProjectId == projectId
				&& lineItem.Timesheet.WeekStartDate == weekStart
				&& lineItem.Timesheet.Status == "SUBMITTED")
			.SumAsync(lineItem => lineItem.HoursLogged, cancellationToken);
	}

	public async Task InsertMissedAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		_context.Timesheets.Add(new Timesheet
		{
			ResourceProfileId = resourceProfileId,
			WeekStartDate = weekStart,
			Status = "MISSED",
			TotalHours = 0,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<Timesheet> AddSubmittedAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
	{
		_context.Timesheets.Add(timesheet);
		await _context.SaveChangesAsync(cancellationToken);
		return timesheet;
	}

	public async Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> GetRecentActivityTagsByResourceProfilesAsync(
		IReadOnlyList<long> resourceProfileIds,
		DateOnly sinceWeekStart,
		CancellationToken cancellationToken = default)
	{
		if (resourceProfileIds.Count == 0)
		{
			return new Dictionary<long, IReadOnlyList<string>>();
		}

		var tagRows = await _context.TimesheetLineItemActivityTags
			.Where(tag =>
				resourceProfileIds.Contains(tag.TimesheetLineItem.Timesheet.ResourceProfileId)
				&& tag.TimesheetLineItem.Timesheet.WeekStartDate >= sinceWeekStart)
			.Select(tag => new
			{
				ResourceProfileId = tag.TimesheetLineItem.Timesheet.ResourceProfileId,
				TagName = tag.ActivityTag.TagName
			})
			.ToListAsync(cancellationToken);

		return tagRows
			.GroupBy(row => row.ResourceProfileId)
			.ToDictionary(
				group => group.Key,
				group => (IReadOnlyList<string>)group.Select(row => row.TagName).Distinct().ToList());
	}

	public async Task<TimesheetComplianceTracking> GetOrCreateComplianceTrackingAsync(
		long resourceProfileId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		var tracking = await _context.TimesheetComplianceTrackings.FindAsync(
			[resourceProfileId, weekStart],
			cancellationToken);

		if (tracking != null)
		{
			return tracking;
		}

		tracking = new TimesheetComplianceTracking
		{
			ResourceProfileId = resourceProfileId,
			WeekStartDate = weekStart
		};
		_context.TimesheetComplianceTrackings.Add(tracking);
		await _context.SaveChangesAsync(cancellationToken);
		return tracking;
	}

	public async Task ClearComplianceFreezeAsync(long resourceProfileId, CancellationToken cancellationToken = default)
	{
		var trackings = await _context.TimesheetComplianceTrackings
			.Where(item => item.ResourceProfileId == resourceProfileId)
			.ToListAsync(cancellationToken);

		foreach (var tracking in trackings)
		{
			tracking.IsFrozenForWeek = false;
		}
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
