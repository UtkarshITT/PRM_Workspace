using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Extensions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Ai;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Services.Interfaces;
using FluentValidation;

namespace PRM.Server.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
public class AiController : ControllerBase
{
	private readonly IAiIntegrationService _aiIntegrationService;
	private readonly IValidator<TeamBuilderRequestDto> _teamBuilderValidator;

	public AiController(
		IAiIntegrationService aiIntegrationService,
		IValidator<TeamBuilderRequestDto> teamBuilderValidator)
	{
		_aiIntegrationService = aiIntegrationService;
		_teamBuilderValidator = teamBuilderValidator;
	}

	[HttpGet("skill-match")]
	public async Task<ActionResult<ApiResponse<AiSkillMatchResponseDto>>> GetSkillMatch(
		[FromQuery(Name = "req")] string requirement,
		CancellationToken cancellationToken)
	{
		var result = await _aiIntegrationService.GetSkillMatchAsync(requirement, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<AiSkillMatchResponseDto>.Ok(result, "Skill match completed."));
	}

	[HttpGet("risk-summary/{projectId:long}")]
	public async Task<ActionResult<ApiResponse<AiRiskSummaryResponseDto>>> GetRiskSummary(
		long projectId,
		CancellationToken cancellationToken)
	{
		var result = await _aiIntegrationService.GetProjectRiskSummaryAsync(projectId, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<AiRiskSummaryResponseDto>.Ok(result, "Risk summary completed."));
	}

	[HttpPost("team-builder")]
	public async Task<ActionResult<ApiResponse<TeamBuilderResponseDto>>> BuildTeam(
		[FromBody] TeamBuilderRequestDto request,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_teamBuilderValidator, request, cancellationToken);
		var result = await _aiIntegrationService.BuildTeamAsync(request, User.GetUserId(), cancellationToken);
		return Ok(ApiResponse<TeamBuilderResponseDto>.Ok(result, "Team builder completed."));
	}
}
