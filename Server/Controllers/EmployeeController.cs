using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Employees;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/employees")]
public class EmployeeController : ControllerBase
{
	private readonly IEmployeeService _employeeService;
	private readonly IValidator<UpdateEmployeeDto> _updateEmployeeValidator;
	private readonly IValidator<AddSkillDto> _addSkillValidator;
	private readonly IValidator<AssignManagerDto> _assignManagerValidator;

	public EmployeeController(
		IEmployeeService employeeService,
		IValidator<UpdateEmployeeDto> updateEmployeeValidator,
		IValidator<AddSkillDto> addSkillValidator,
		IValidator<AssignManagerDto> assignManagerValidator)
	{
		_employeeService = employeeService;
		_updateEmployeeValidator = updateEmployeeValidator;
		_addSkillValidator = addSkillValidator;
		_assignManagerValidator = assignManagerValidator;
	}

	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeListItemDto>>>> GetAllEmployees(
		[FromQuery] string? status,
		[FromQuery] string? department,
		CancellationToken cancellationToken)
	{
		var employees = await _employeeService.GetAllEmployeesAsync(status, department, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<EmployeeListItemDto>>.Ok(employees, "Employees retrieved."));
	}

	[HttpPut("{id:long}")]
	public async Task<ActionResult<ApiResponse<object>>> UpdateEmployee(
		long id,
		[FromBody] UpdateEmployeeDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_updateEmployeeValidator, dto, cancellationToken);
		await _employeeService.UpdateEmployeeAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Employee updated."));
	}

	[HttpPut("{id:long}/deactivate")]
	public async Task<ActionResult<ApiResponse<object>>> DeactivateEmployee(long id, CancellationToken cancellationToken)
	{
		await _employeeService.DeactivateEmployeeAsync(id, GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Employee deactivated."));
	}

	[HttpPost("{id:long}/skills")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeSkillDto>>>> AddSkill(
		long id,
		[FromBody] AddSkillDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_addSkillValidator, dto, cancellationToken);
		var skills = await _employeeService.AddSkillAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<EmployeeSkillDto>>.Ok(skills, "Skill added."));
	}

	[HttpDelete("{id:long}/skills/{skillId:long}")]
	public async Task<ActionResult<ApiResponse<object>>> RemoveSkill(
		long id,
		long skillId,
		CancellationToken cancellationToken)
	{
		await _employeeService.RemoveSkillAsync(id, skillId, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Skill removed."));
	}

	[HttpPut("{id:long}/manager")]
	public async Task<ActionResult<ApiResponse<object>>> AssignManager(
		long id,
		[FromBody] AssignManagerDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_assignManagerValidator, dto, cancellationToken);
		await _employeeService.AssignManagerAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Manager assigned."));
	}

	private long GetUserId()
	{
		var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(userIdClaim))
		{
			throw new InvalidOperationException("User identifier claim is missing.");
		}

		return long.Parse(userIdClaim);
	}
}
