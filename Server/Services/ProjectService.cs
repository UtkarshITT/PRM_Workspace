using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IProjectService
{
	Task<ProjectCreatedDto> CreateProjectAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectListItemDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
	Task UpdateProjectAsync(long projectId, UpdateProjectDto dto, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<MilestoneListItemDto>> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default);
	Task<MilestoneListItemDto> AddMilestoneAsync(long projectId, CreateMilestoneDto dto, CancellationToken cancellationToken = default);
	Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusDto dto, CancellationToken cancellationToken = default);
}

public class ProjectService : IProjectService
{
	private readonly IProjectRepository _projectRepository;
	private readonly IUserRepository _userRepository;

	public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
	{
		_projectRepository = projectRepository;
		_userRepository = userRepository;
	}

	public async Task<ProjectCreatedDto> CreateProjectAsync(
		CreateProjectDto dto,
		CancellationToken cancellationToken = default)
	{
		await ValidateManagerAsync(dto.ManagerUserId, cancellationToken);

		var now = DateTime.UtcNow;
		var project = new Project
		{
			ProjectName = dto.ProjectName,
			Description = dto.Description,
			StartDate = dto.StartDate,
			EndDate = dto.EndDate,
			ProjectStatus = dto.ProjectStatus,
			HealthStatus = "GREEN",
			TotalStoryPoints = dto.TotalStoryPoints,
			ManagerUserId = dto.ManagerUserId,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		await _projectRepository.AddAsync(project, cancellationToken);
		project.ProjectCode = $"PRJ-{project.Id:D6}";
		await _projectRepository.SaveChangesAsync(cancellationToken);

		return new ProjectCreatedDto
		{
			ProjectId = project.Id,
			ProjectCode = project.ProjectCode
		};
	}

	public async Task<IReadOnlyList<ProjectListItemDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
	{
		var projects = await _projectRepository.GetAllAsync(cancellationToken);
		return projects.Select(MapListItem).ToList();
	}

	public async Task UpdateProjectAsync(
		long projectId,
		UpdateProjectDto dto,
		CancellationToken cancellationToken = default)
	{
		var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

		if (project == null || !project.IsActive)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}

		await ValidateManagerAsync(dto.ManagerUserId, cancellationToken);
		ValidateMilestonesWithinProjectDates(project.Milestones, dto.StartDate, dto.EndDate);

		project.ProjectName = dto.ProjectName;
		project.Description = dto.Description;
		project.StartDate = dto.StartDate;
		project.EndDate = dto.EndDate;
		project.ProjectStatus = dto.ProjectStatus;
		project.TotalStoryPoints = dto.TotalStoryPoints;
		project.ManagerUserId = dto.ManagerUserId;
		project.UpdatedAt = DateTime.UtcNow;

		await _projectRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<MilestoneListItemDto>> GetMilestonesAsync(
		long projectId,
		CancellationToken cancellationToken = default)
	{
		await EnsureProjectExistsAsync(projectId, cancellationToken);
		var milestones = await _projectRepository.GetMilestonesAsync(projectId, cancellationToken);
		return milestones.Select(MapMilestone).ToList();
	}

	public async Task<MilestoneListItemDto> AddMilestoneAsync(
		long projectId,
		CreateMilestoneDto dto,
		CancellationToken cancellationToken = default)
	{
		var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

		if (project == null || !project.IsActive)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}

		ValidateMilestoneDueDate(project.StartDate, project.EndDate, dto.DueDate);

		var now = DateTime.UtcNow;
		var milestone = new ProjectMilestone
		{
			ProjectId = projectId,
			MilestoneTitle = dto.MilestoneTitle,
			DueDate = dto.DueDate,
			StoryPoints = dto.StoryPoints,
			SortOrder = dto.SortOrder,
			MilestoneStatus = MilestoneStatuses.NotStarted,
			CreatedAt = now,
			UpdatedAt = now
		};

		await _projectRepository.AddMilestoneAsync(milestone, cancellationToken);
		return MapMilestone(milestone);
	}

	public async Task UpdateMilestoneStatusAsync(
		long projectId,
		long milestoneId,
		UpdateMilestoneStatusDto dto,
		CancellationToken cancellationToken = default)
	{
		await EnsureProjectExistsAsync(projectId, cancellationToken);

		var milestone = await _projectRepository.GetMilestoneAsync(projectId, milestoneId, cancellationToken);

		if (milestone == null)
		{
			throw new NotFoundException($"Milestone {milestoneId} was not found for project {projectId}.");
		}

		milestone.MilestoneStatus = dto.MilestoneStatus;
		milestone.UpdatedAt = DateTime.UtcNow;
		milestone.CompletedAt = dto.MilestoneStatus == MilestoneStatuses.Done
			? DateTime.UtcNow
			: null;

		await _projectRepository.SaveChangesAsync(cancellationToken);
	}

	private async Task ValidateManagerAsync(long managerUserId, CancellationToken cancellationToken)
	{
		var manager = await _userRepository.GetByIdAsync(managerUserId, cancellationToken);

		if (manager == null || !manager.IsActive)
		{
			throw new NotFoundException($"Manager user with ID {managerUserId} was not found.");
		}

		if (manager.Role != Roles.Manager)
		{
			throw new ValidationException("Specified user is not a manager.");
		}
	}

	private async Task EnsureProjectExistsAsync(long projectId, CancellationToken cancellationToken)
	{
		var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

		if (project == null)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}
	}

	private static void ValidateMilestoneDueDate(DateOnly projectStart, DateOnly projectEnd, DateOnly dueDate)
	{
		if (dueDate < projectStart || dueDate > projectEnd)
		{
			throw new ValidationException(
				$"Milestone due date must be between {projectStart:yyyy-MM-dd} and {projectEnd:yyyy-MM-dd}.");
		}
	}

	private static void ValidateMilestonesWithinProjectDates(
		IEnumerable<ProjectMilestone> milestones,
		DateOnly newStart,
		DateOnly newEnd)
	{
		foreach (var milestone in milestones)
		{
			if (milestone.DueDate < newStart || milestone.DueDate > newEnd)
			{
				throw new ValidationException(
					$"Cannot update project dates: milestone '{milestone.MilestoneTitle}' due date falls outside the new range.");
			}
		}
	}

	private static ProjectListItemDto MapListItem(Project project)
	{
		var storyPointsDone = project.Milestones
			.Where(milestone => milestone.MilestoneStatus == MilestoneStatuses.Done)
			.Sum(milestone => milestone.StoryPoints);

		return new ProjectListItemDto
		{
			Id = project.Id,
			ProjectCode = project.ProjectCode,
			ProjectName = project.ProjectName,
			ManagerName = project.ManagerUser.FullName,
			EndDate = project.EndDate,
			ProjectStatus = project.ProjectStatus,
			StoryPointsDone = storyPointsDone,
			TotalStoryPoints = project.TotalStoryPoints
		};
	}

	private static MilestoneListItemDto MapMilestone(ProjectMilestone milestone)
	{
		return new MilestoneListItemDto
		{
			Id = milestone.Id,
			SortOrder = milestone.SortOrder,
			MilestoneTitle = milestone.MilestoneTitle,
			DueDate = milestone.DueDate,
			StoryPoints = milestone.StoryPoints,
			MilestoneStatus = milestone.MilestoneStatus
		};
	}
}
