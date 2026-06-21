using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Extensions;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.SystemConfig;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ApiController]
[Route("api/system-config")]
public class SystemConfigController : ControllerBase
{
	private readonly ISystemConfigService _systemConfigService;

	public SystemConfigController(ISystemConfigService systemConfigService)
	{
		_systemConfigService = systemConfigService;
	}

	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemConfigItemDto>>>> GetAll(
		CancellationToken cancellationToken)
	{
		var result = await _systemConfigService.GetAllAsync(cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<SystemConfigItemDto>>.Ok(result, "System configuration retrieved."));
	}

	[HttpPut]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemConfigItemDto>>>> Update(
		[FromBody] UpdateSystemConfigDto dto,
		CancellationToken cancellationToken)
	{
		var result = await _systemConfigService.UpdateAsync(dto, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<SystemConfigItemDto>>.Ok(result, "System configuration updated."));
	}
}
