using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
	private readonly IProjectService _projectService;
	private readonly IValidator<CreateProjectDto> _createProjectValidator;
	private readonly IValidator<UpdateProjectDto> _updateProjectValidator;
	private readonly IValidator<CreateMilestoneDto> _createMilestoneValidator;
	private readonly IValidator<UpdateMilestoneStatusDto> _updateMilestoneStatusValidator;

	public ProjectController(
		IProjectService projectService,
		IValidator<CreateProjectDto> createProjectValidator,
		IValidator<UpdateProjectDto> updateProjectValidator,
		IValidator<CreateMilestoneDto> createMilestoneValidator,
		IValidator<UpdateMilestoneStatusDto> updateMilestoneStatusValidator)
	{
		_projectService = projectService;
		_createProjectValidator = createProjectValidator;
		_updateProjectValidator = updateProjectValidator;
		_createMilestoneValidator = createMilestoneValidator;
		_updateMilestoneStatusValidator = updateMilestoneStatusValidator;
	}

	[HttpPost]
	public async Task<ActionResult<ApiResponse<ProjectCreatedDto>>> CreateProject(
		[FromBody] CreateProjectDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_createProjectValidator, dto, cancellationToken);
		var result = await _projectService.CreateProjectAsync(dto, cancellationToken);
		return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectCreatedDto>.Ok(result, "Project created."));
	}

	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectListItemDto>>>> GetAllProjects(
		CancellationToken cancellationToken)
	{
		var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<ProjectListItemDto>>.Ok(projects, "Projects retrieved."));
	}

	[HttpPut("{id:long}")]
	public async Task<ActionResult<ApiResponse<object>>> UpdateProject(
		long id,
		[FromBody] UpdateProjectDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_updateProjectValidator, dto, cancellationToken);
		await _projectService.UpdateProjectAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Project updated."));
	}

	[HttpGet("{id:long}/milestones")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<MilestoneListItemDto>>>> GetMilestones(
		long id,
		CancellationToken cancellationToken)
	{
		var milestones = await _projectService.GetMilestonesAsync(id, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<MilestoneListItemDto>>.Ok(milestones, "Milestones retrieved."));
	}

	[HttpPost("{id:long}/milestones")]
	public async Task<ActionResult<ApiResponse<MilestoneListItemDto>>> AddMilestone(
		long id,
		[FromBody] CreateMilestoneDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_createMilestoneValidator, dto, cancellationToken);
		var milestone = await _projectService.AddMilestoneAsync(id, dto, cancellationToken);
		return StatusCode(StatusCodes.Status201Created, ApiResponse<MilestoneListItemDto>.Ok(milestone, "Milestone added."));
	}

	[HttpPut("{id:long}/milestones/{milestoneId:long}")]
	public async Task<ActionResult<ApiResponse<object>>> UpdateMilestoneStatus(
		long id,
		long milestoneId,
		[FromBody] UpdateMilestoneStatusDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_updateMilestoneStatusValidator, dto, cancellationToken);
		await _projectService.UpdateMilestoneStatusAsync(id, milestoneId, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Milestone updated."));
	}
}
