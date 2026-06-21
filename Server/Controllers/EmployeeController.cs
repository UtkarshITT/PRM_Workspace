using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Extensions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Employees;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeeController : ControllerBase
{
	private readonly IResourceProfileService _resourceProfileService;
	private readonly IValidator<UpdateEmployeeDto> _updateEmployeeValidator;
	private readonly IValidator<AddSkillDto> _addSkillValidator;
	private readonly IValidator<AssignManagerDto> _assignManagerValidator;

	public EmployeeController(
		IResourceProfileService resourceProfileService,
		IValidator<UpdateEmployeeDto> updateEmployeeValidator,
		IValidator<AddSkillDto> addSkillValidator,
		IValidator<AssignManagerDto> assignManagerValidator)
	{
		_resourceProfileService = resourceProfileService;
		_updateEmployeeValidator = updateEmployeeValidator;
		_addSkillValidator = addSkillValidator;
		_assignManagerValidator = assignManagerValidator;
	}

	[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
	[HttpGet("my-team")]
	public async Task<ActionResult<ApiResponse<TeamDashboardDto>>> GetMyTeam(CancellationToken cancellationToken)
	{
		var dashboard = await _resourceProfileService.GetTeamDashboardAsync(User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<TeamDashboardDto>.Ok(dashboard, "Team dashboard retrieved."));
	}

	[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<ApiResponse<TeamMemberDetailDto>>> GetTeamMember(long id, CancellationToken cancellationToken)
	{
		var detail = await _resourceProfileService.GetTeamMemberDetailAsync(id, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<TeamMemberDetailDto>.Ok(detail, "Employee detail retrieved."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeListItemDto>>>> GetAllEmployees(
		[FromQuery] string? status,
		[FromQuery] string? department,
		CancellationToken cancellationToken)
	{
		var employees = await _resourceProfileService.GetAllEmployeesAsync(status, department, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<EmployeeListItemDto>>.Ok(employees, "Employees retrieved."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPut("{id:long}")]
	public async Task<ActionResult<ApiResponse<object>>> UpdateEmployee(
		long id,
		[FromBody] UpdateEmployeeDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_updateEmployeeValidator, dto, cancellationToken);
		await _resourceProfileService.UpdateEmployeeAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Employee updated."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPut("{id:long}/deactivate")]
	public async Task<ActionResult<ApiResponse<object>>> DeactivateEmployee(long id, CancellationToken cancellationToken)
	{
		await _resourceProfileService.DeactivateEmployeeAsync(id, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Employee deactivated."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPost("{id:long}/skills")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeSkillDto>>>> AddSkill(
		long id,
		[FromBody] AddSkillDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_addSkillValidator, dto, cancellationToken);
		var skills = await _resourceProfileService.AddSkillAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<EmployeeSkillDto>>.Ok(skills, "Skill added."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpDelete("{id:long}/skills/{skillId:long}")]
	public async Task<ActionResult<ApiResponse<object>>> RemoveSkill(
		long id,
		long skillId,
		CancellationToken cancellationToken)
	{
		await _resourceProfileService.RemoveSkillAsync(id, skillId, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Skill removed."));
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPut("{id:long}/manager")]
	public async Task<ActionResult<ApiResponse<object>>> AssignManager(
		long id,
		[FromBody] AssignManagerDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_assignManagerValidator, dto, cancellationToken);
		await _resourceProfileService.AssignManagerAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Manager assigned."));
	}

	[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
	[HttpPut("{id:long}/restore-timesheet-access")]
	public async Task<ActionResult<ApiResponse<object>>> RestoreTimesheetAccess(
		long id,
		CancellationToken cancellationToken)
	{
		await _resourceProfileService.RestoreTimesheetAccessAsync(id, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Timesheet access restored."));
	}
}
