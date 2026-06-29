using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Extensions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Allocations;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[ApiController]
[Route("api/allocations")]
public class AllocationController : ControllerBase
{
	private readonly IAllocationService _allocationService;
	private readonly IValidator<CreateAllocationDto> _createAllocationValidator;

	public AllocationController(
		IAllocationService allocationService,
		IValidator<CreateAllocationDto> createAllocationValidator)
	{
		_allocationService = allocationService;
		_createAllocationValidator = createAllocationValidator;
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<AllocationListItemDto>>>> GetAllAllocations(
		[FromQuery] long? employeeId,
		[FromQuery] long? projectId,
		[FromQuery] string? status,
		CancellationToken cancellationToken)
	{
		var allocations = await _allocationService.GetAllAllocationsAsync(employeeId, projectId, status, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<AllocationListItemDto>>.Ok(allocations, "Allocations retrieved."));
	}

	[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
	[HttpPost]
	public async Task<ActionResult<ApiResponse<AllocationCreatedDto>>> CreateAllocation(
		[FromBody] CreateAllocationDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_createAllocationValidator, dto, cancellationToken);
		var result = await _allocationService.CreateAllocationAsync(dto, User.GetUserId(), cancellationToken);
		return StatusCode(StatusCodes.Status201Created, ApiResponse<AllocationCreatedDto>.Ok(result, "Allocation created."));
	}

	[Authorize(Policy = AuthorizationPolicies.EmployeeOnly)]
	[HttpGet("my")]
	public async Task<ActionResult<ApiResponse<EmployeeAllocationsResponseDto>>> GetMyAllocations(
		[FromQuery] DateOnly? week,
		CancellationToken cancellationToken)
	{
		var result = await _allocationService.GetMyAllocationsAsync(User.GetResourceProfileId(), week, cancellationToken);
		return Ok(ApiResponse<EmployeeAllocationsResponseDto>.Ok(result, "Allocations retrieved."));
	}

	[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
	[HttpPut("{id:long}/end")]
	public async Task<ActionResult<ApiResponse<object>>> EndAllocation(long id, CancellationToken cancellationToken)
	{
		await _allocationService.EndAllocationAsync(id, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Allocation ended."));
	}
}
