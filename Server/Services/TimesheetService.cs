using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Timesheets;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface ITimesheetService
{
	Task<TimesheetSubmittedDto> SubmitTimesheetAsync(
		long employeeId,
		SubmitTimesheetDto dto,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<TimesheetListItemDto>> GetMyTimesheetsAsync(
		long employeeId,
		CancellationToken cancellationToken = default);
	Task<TimesheetDetailDto> GetMyTimesheetDetailAsync(
		long employeeId,
		long timesheetId,
		CancellationToken cancellationToken = default);
	Task<TimesheetRemindersDto> GetRemindersAsync(
		long employeeId,
		CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ActivityTagDto>> GetActivityTagsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<TeamTimesheetRowDto>> GetTeamTimesheetsAsync(
		long managerUserId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default);
	Task<TimesheetDetailDto> GetTimesheetDetailForManagerAsync(
		long timesheetId,
		long managerUserId,
		CancellationToken cancellationToken = default);
	Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default);
}

public class TimesheetService : ITimesheetService
{
	private const decimal DefaultMaxWeeklyHours = 40;
	private const string OtherTagCode = "OTHER";

	private readonly ITimesheetRepository _timesheetRepository;
	private readonly IAllocationRepository _allocationRepository;
	private readonly IActivityTagRepository _activityTagRepository;
	private readonly ISystemConfigRepository _systemConfigRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IProjectRepository _projectRepository;
	private readonly IAuditService _auditService;

	public TimesheetService(
		ITimesheetRepository timesheetRepository,
		IAllocationRepository allocationRepository,
		IActivityTagRepository activityTagRepository,
		ISystemConfigRepository systemConfigRepository,
		IResourceProfileRepository resourceProfileRepository,
		IProjectRepository projectRepository,
		IAuditService auditService)
	{
		_timesheetRepository = timesheetRepository;
		_allocationRepository = allocationRepository;
		_activityTagRepository = activityTagRepository;
		_systemConfigRepository = systemConfigRepository;
		_resourceProfileRepository = resourceProfileRepository;
		_projectRepository = projectRepository;
		_auditService = auditService;
	}

	public async Task<TimesheetSubmittedDto> SubmitTimesheetAsync(
		long employeeId,
		SubmitTimesheetDto dto,
		CancellationToken cancellationToken = default)
	{
		var weekStart = dto.WeekStartDate;
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(employeeId, cancellationToken);
		if (resourceProfile == null || !resourceProfile.IsActive)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		if (resourceProfile.IsTimesheetFrozen)
		{
			throw new ValidationException("Timesheet access is frozen. Contact your manager to restore access.");
		}

		if (weekStart.DayOfWeek != DayOfWeek.Monday)
		{
			throw new ValidationException("Week start date must be a Monday.");
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		if (weekStart > WeekHelper.GetWeekStart(today))
		{
			throw new FutureWeekException();
		}

		if (await _timesheetRepository.ExistsForWeekAsync(employeeId, weekStart, cancellationToken))
		{
			throw new DuplicateTimesheetException(weekStart);
		}

		var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
		var weekEnd = WeekHelper.GetWeekEnd(weekStart);
		var weekAllocations = await GetWeekAllocationsAsync(employeeId, weekStart, weekEnd, cancellationToken);
		var allocatedProjectIds = weekAllocations.Select(allocation => allocation.ProjectId).ToHashSet();

		if (dto.LineItems.Select(item => item.ProjectId).Distinct().Count() != dto.LineItems.Count)
		{
			throw new ValidationException("Duplicate project entries are not allowed.");
		}

		var tagIds = dto.LineItems.SelectMany(item => item.ActivityTagIds).Distinct().ToList();
		var tags = await _activityTagRepository.GetByIdsAsync(tagIds, cancellationToken);

		if (tags.Count != tagIds.Count)
		{
			throw new ValidationException("One or more activity tags are invalid.");
		}

		var otherTagId = tags.FirstOrDefault(tag => tag.TagCode == OtherTagCode)?.Id;

		foreach (var lineItem in dto.LineItems)
		{
			if (!allocatedProjectIds.Contains(lineItem.ProjectId))
			{
				throw new ValidationException($"Project {lineItem.ProjectId} is not an active allocation for this week.");
			}

			var allocationPercent = weekAllocations
				.Where(allocation => allocation.ProjectId == lineItem.ProjectId)
				.Sum(allocation => allocation.AllocationPercentage);
			var maxHoursForProject = allocationPercent / 100m * maxWeeklyHours;

			if (lineItem.HoursLogged > maxHoursForProject)
			{
				throw new ValidationException(
					$"Hours for project exceed allocation cap ({maxHoursForProject:0.##} hrs max).");
			}

			if (otherTagId.HasValue
				&& lineItem.ActivityTagIds.Contains(otherTagId.Value)
				&& string.IsNullOrWhiteSpace(lineItem.CustomTagText))
			{
				throw new ValidationException("Custom tag text is required when 'Other' is selected.");
			}
		}

		var totalHours = dto.LineItems.Sum(item => item.HoursLogged);
		if (totalHours > maxWeeklyHours)
		{
			throw new ValidationException(
				$"Total hours ({totalHours:0.##}) exceed maximum weekly hours ({maxWeeklyHours:0.##}).");
		}

		var now = DateTime.UtcNow;
		var timesheet = new Timesheet
		{
			ResourceProfileId = employeeId,
			WeekStartDate = weekStart,
			Status = "SUBMITTED",
			TotalHours = totalHours,
			Remarks = dto.Remarks,
			SubmittedAt = now,
			CreatedAt = now,
			UpdatedAt = now
		};

		foreach (var lineItemDto in dto.LineItems)
		{
			var lineItem = new TimesheetLineItem
			{
				ProjectId = lineItemDto.ProjectId,
				HoursLogged = lineItemDto.HoursLogged,
				CreatedAt = now,
				UpdatedAt = now
			};

			foreach (var tagId in lineItemDto.ActivityTagIds)
			{
				lineItem.ActivityTags.Add(new TimesheetLineItemActivityTag
				{
					ActivityTagId = tagId,
					CustomTagText = tagId == otherTagId ? lineItemDto.CustomTagText?.Trim() : null
				});
			}

			timesheet.LineItems.Add(lineItem);
		}

		await _timesheetRepository.AddSubmittedAsync(timesheet, cancellationToken);
		await _auditService.LogAsync(
			resourceProfile.UserId,
			"SUBMIT",
			"TIMESHEETS",
			timesheet.Id,
			"Timesheet submitted",
			newValues: $"{{\"resourceProfileId\":{employeeId},\"weekStartDate\":\"{weekStart:yyyy-MM-dd}\",\"totalHours\":{totalHours}}}",
			cancellationToken: cancellationToken);

		return new TimesheetSubmittedDto
		{
			TimesheetId = timesheet.Id,
			WeekStartDate = weekStart,
			Status = timesheet.Status,
			TotalHours = totalHours
		};
	}

	public async Task<IReadOnlyList<TimesheetListItemDto>> GetMyTimesheetsAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		var timesheets = await _timesheetRepository.GetByResourceProfileIdAsync(employeeId, cancellationToken);

		return timesheets.Select(timesheet => new TimesheetListItemDto
		{
			Id = timesheet.Id,
			WeekStartDate = timesheet.WeekStartDate,
			TotalHours = timesheet.TotalHours,
			Status = timesheet.Status
		}).ToList();
	}

	public async Task<TimesheetDetailDto> GetMyTimesheetDetailAsync(
		long employeeId,
		long timesheetId,
		CancellationToken cancellationToken = default)
	{
		var timesheet = await _timesheetRepository.GetDetailByIdForResourceProfileAsync(timesheetId, employeeId, cancellationToken);

		if (timesheet == null)
		{
			throw new NotFoundException($"Timesheet with ID {timesheetId} was not found.");
		}

		return MapTimesheetDetail(timesheet);
	}

	public async Task<TimesheetRemindersDto> GetRemindersAsync(
		long employeeId,
		CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var lastCompletedWeek = WeekHelper.GetLastCompletedWeekStart(today);
		var lastCompletedWeekEnd = WeekHelper.GetWeekEnd(lastCompletedWeek);
		var messages = new List<string>();
		var weekAllocations = await GetWeekAllocationsAsync(
			employeeId,
			lastCompletedWeek,
			lastCompletedWeekEnd,
			cancellationToken);

		var hasTimesheet = await _timesheetRepository.ExistsForWeekAsync(employeeId, lastCompletedWeek, cancellationToken);
		if (weekAllocations.Count > 0 && !hasTimesheet)
		{
			messages.Add(
				$"Reminder: Timesheet for week {lastCompletedWeek:dd-MMM-yyyy} has not been submitted.");
		}

		var timesheets = await _timesheetRepository.GetByResourceProfileIdAsync(employeeId, cancellationToken);
		foreach (var missed in timesheets.Where(timesheet => timesheet.Status == "MISSED"))
		{
			messages.Add($"Reminder: Timesheet for week {missed.WeekStartDate:dd-MMM-yyyy} was missed.");
		}

		return new TimesheetRemindersDto { Messages = messages.Distinct().ToList() };
	}

	public async Task<IReadOnlyList<ActivityTagDto>> GetActivityTagsAsync(CancellationToken cancellationToken = default)
	{
		var tags = await _activityTagRepository.GetAllActiveAsync(cancellationToken);

		return tags.Select(tag => new ActivityTagDto
		{
			Id = tag.Id,
			TagCode = tag.TagCode,
			TagName = tag.TagName
		}).ToList();
	}

	public async Task<IReadOnlyList<TeamTimesheetRowDto>> GetTeamTimesheetsAsync(
		long managerUserId,
		DateOnly weekStart,
		CancellationToken cancellationToken = default)
	{
		if (weekStart.DayOfWeek != DayOfWeek.Monday)
		{
			throw new ValidationException("Week must start on a Monday.");
		}

		var teamMembers = await _resourceProfileRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
		var teamResourceProfileIds = teamMembers.Select(resourceProfile => resourceProfile.Id).ToList();
		var managerProjectIds = (await _projectRepository.GetByManagerUserIdAsync(managerUserId, cancellationToken))
			.Select(project => project.Id)
			.ToHashSet();

		var timesheets = await _timesheetRepository.GetByTeamAndWeekAsync(teamResourceProfileIds, weekStart, cancellationToken);
		var weekEnd = WeekHelper.GetWeekEnd(weekStart);
		var rows = new List<TeamTimesheetRowDto>();

		foreach (var timesheet in timesheets)
		{
			if (timesheet.Status == "SUBMITTED")
			{
				foreach (var lineItem in timesheet.LineItems.Where(item => managerProjectIds.Contains(item.ProjectId)))
				{
					rows.Add(new TeamTimesheetRowDto
					{
						TimesheetId = timesheet.Id,
						EmployeeName = timesheet.ResourceProfile.User.FullName,
						ProjectName = lineItem.Project.ProjectName,
						HoursLogged = lineItem.HoursLogged,
						Status = timesheet.Status
					});
				}
			}
			else if (timesheet.Status == "MISSED")
			{
				var allocations = await _allocationRepository.GetActiveByResourceProfileIdAsync(
					timesheet.ResourceProfileId,
					cancellationToken);

				foreach (var allocation in allocations.Where(allocation =>
					         managerProjectIds.Contains(allocation.ProjectId)
					         && UtilizationCalculator.PeriodsOverlap(
						         allocation.AllocationStartDate,
						         allocation.AllocationEndDate,
						         weekStart,
						         weekEnd)))
				{
					var project = await _projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
					rows.Add(new TeamTimesheetRowDto
					{
						TimesheetId = timesheet.Id,
						EmployeeName = timesheet.ResourceProfile.User.FullName,
						ProjectName = project?.ProjectName ?? "Unknown",
						HoursLogged = 0,
						Status = timesheet.Status
					});
				}
			}
		}

		return rows
			.OrderBy(row => row.EmployeeName)
			.ThenBy(row => row.ProjectName)
			.ToList();
	}

	public async Task<TimesheetDetailDto> GetTimesheetDetailForManagerAsync(
		long timesheetId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var timesheet = await _timesheetRepository.GetDetailByIdAsync(timesheetId, cancellationToken);

		if (timesheet == null)
		{
			throw new NotFoundException($"Timesheet with ID {timesheetId} was not found.");
		}

		var teamMember = await _resourceProfileRepository.GetTeamMemberAsync(
			timesheet.ResourceProfileId,
			managerUserId,
			cancellationToken);

		if (teamMember == null)
		{
			throw new ValidationException("Timesheet is not for an employee on your team.");
		}

		return MapTimesheetDetail(timesheet);
	}

	public async Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var lastWeekStart = WeekHelper.GetLastCompletedWeekStart(today);
		var lastWeekEnd = WeekHelper.GetWeekEnd(lastWeekStart);
		var allocations = await _allocationRepository.GetActiveAllocationsForWeekAsync(
			lastWeekStart,
			lastWeekEnd,
			cancellationToken);
		var resourceProfileIds = allocations.Select(allocation => allocation.ResourceProfileId).Distinct();
		var createdCount = 0;

		foreach (var resourceProfileId in resourceProfileIds)
		{
			if (await _timesheetRepository.ExistsForWeekAsync(resourceProfileId, lastWeekStart, cancellationToken))
			{
				continue;
			}

			await _timesheetRepository.InsertMissedAsync(resourceProfileId, lastWeekStart, cancellationToken);
			createdCount++;
		}

		return createdCount;
	}

	private async Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
	{
		var value = await _systemConfigRepository.GetValueByKeyAsync(SystemConfigKeys.MaxWeeklyHours, cancellationToken);

		if (decimal.TryParse(value, out var maxHours) && maxHours > 0)
		{
			return maxHours;
		}

		return DefaultMaxWeeklyHours;
	}

	private async Task<IReadOnlyList<ProjectAllocation>> GetWeekAllocationsAsync(
		long employeeId,
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken)
	{
		var activeAllocations = await _allocationRepository.GetActiveByResourceProfileIdAsync(employeeId, cancellationToken);

		return activeAllocations
			.Where(allocation => UtilizationCalculator.PeriodsOverlap(
				allocation.AllocationStartDate,
				allocation.AllocationEndDate,
				weekStart,
				weekEnd))
			.ToList();
	}

	private static TimesheetDetailDto MapTimesheetDetail(Timesheet timesheet)
	{
		return new TimesheetDetailDto
		{
			Id = timesheet.Id,
			WeekStartDate = timesheet.WeekStartDate,
			Status = timesheet.Status,
			TotalHours = timesheet.TotalHours,
			Remarks = timesheet.Remarks,
			LineItems = timesheet.LineItems.Select(lineItem => new TimesheetDetailLineItemDto
			{
				ProjectName = lineItem.Project.ProjectName,
				HoursLogged = lineItem.HoursLogged,
				ActivityTags = lineItem.ActivityTags
					.Select(tag => tag.CustomTagText ?? tag.ActivityTag.TagName)
					.ToList()
			}).ToList()
		};
	}
}
