using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Timesheets;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[ApiController]
[Route("api/timesheets")]
public class TimesheetController : ControllerBase
{
	private readonly ITimesheetService _timesheetService;
	private readonly IValidator<SubmitTimesheetDto> _submitTimesheetValidator;

	public TimesheetController(
		ITimesheetService timesheetService,
		IValidator<SubmitTimesheetDto> submitTimesheetValidator)
	{
		_timesheetService = timesheetService;
		_submitTimesheetValidator = submitTimesheetValidator;
	}

	[Authorize(Roles = "EMPLOYEE")]
	[HttpPost]
	public async Task<ActionResult<ApiResponse<TimesheetSubmittedDto>>> SubmitTimesheet(
		[FromBody] SubmitTimesheetDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_submitTimesheetValidator, dto, cancellationToken);
		var result = await _timesheetService.SubmitTimesheetAsync(GetEmployeeId(), dto, cancellationToken);
		return StatusCode(StatusCodes.Status201Created, ApiResponse<TimesheetSubmittedDto>.Ok(result, "Timesheet submitted."));
	}

	[Authorize(Roles = "EMPLOYEE")]
	[HttpGet("my")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<TimesheetListItemDto>>>> GetMyTimesheets(
		CancellationToken cancellationToken)
	{
		var timesheets = await _timesheetService.GetMyTimesheetsAsync(GetEmployeeId(), cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<TimesheetListItemDto>>.Ok(timesheets, "Timesheets retrieved."));
	}

	[Authorize(Roles = "EMPLOYEE")]
	[HttpGet("my/{id:long}")]
	public async Task<ActionResult<ApiResponse<TimesheetDetailDto>>> GetMyTimesheetDetail(
		long id,
		CancellationToken cancellationToken)
	{
		var detail = await _timesheetService.GetMyTimesheetDetailAsync(GetEmployeeId(), id, cancellationToken);
		return Ok(ApiResponse<TimesheetDetailDto>.Ok(detail, "Timesheet detail retrieved."));
	}

	[Authorize(Roles = "EMPLOYEE")]
	[HttpGet("reminders")]
	public async Task<ActionResult<ApiResponse<TimesheetRemindersDto>>> GetReminders(CancellationToken cancellationToken)
	{
		var reminders = await _timesheetService.GetRemindersAsync(GetEmployeeId(), cancellationToken);
		return Ok(ApiResponse<TimesheetRemindersDto>.Ok(reminders, "Reminders retrieved."));
	}

	[Authorize(Roles = "EMPLOYEE")]
	[HttpGet("activity-tags")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityTagDto>>>> GetActivityTags(
		CancellationToken cancellationToken)
	{
		var tags = await _timesheetService.GetActivityTagsAsync(cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<ActivityTagDto>>.Ok(tags, "Activity tags retrieved."));
	}

	[Authorize(Roles = "MANAGER")]
	[HttpGet("team")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<TeamTimesheetRowDto>>>> GetTeamTimesheets(
		[FromQuery] DateOnly week,
		CancellationToken cancellationToken)
	{
		var rows = await _timesheetService.GetTeamTimesheetsAsync(GetUserId(), week, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<TeamTimesheetRowDto>>.Ok(rows, "Team timesheets retrieved."));
	}

	[Authorize(Roles = "MANAGER")]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<ApiResponse<TimesheetDetailDto>>> GetTimesheetDetail(
		long id,
		CancellationToken cancellationToken)
	{
		var detail = await _timesheetService.GetTimesheetDetailForManagerAsync(id, GetUserId(), cancellationToken);
		return Ok(ApiResponse<TimesheetDetailDto>.Ok(detail, "Timesheet detail retrieved."));
	}

	private long GetEmployeeId()
	{
		var employeeIdClaim = User.FindFirstValue("employee_id");
		return long.Parse(employeeIdClaim!);
	}

	private long GetUserId()
	{
		var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return long.Parse(userIdClaim!);
	}
}
