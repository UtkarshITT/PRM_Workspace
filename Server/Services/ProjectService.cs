using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IProjectService
{
	Task<ProjectCreatedDto> CreateProjectAsync(CreateProjectDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ProjectListItemDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
	Task UpdateProjectAsync(long projectId, UpdateProjectDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<MilestoneListItemDto>> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default);
	Task<MilestoneListItemDto> AddMilestoneAsync(long projectId, CreateMilestoneDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ManagerProjectListItemDto>> GetMyProjectsAsync(
		long managerUserId,
		CancellationToken cancellationToken = default);
	Task<ManagerProjectDetailDto> GetProjectDetailAsync(
		long projectId,
		long managerUserId,
		CancellationToken cancellationToken = default);
	Task EvaluateAllProjectsHealthAsync(CancellationToken cancellationToken = default);
}

public class ProjectService : IProjectService
{
	private const decimal DefaultMaxWeeklyHours = 40;

	private readonly IProjectRepository _projectRepository;
	private readonly IUserRepository _userRepository;
	private readonly ITimesheetRepository _timesheetRepository;
	private readonly ISystemConfigRepository _systemConfigRepository;
	private readonly IAuditService _auditService;

	public ProjectService(
		IProjectRepository projectRepository,
		IUserRepository userRepository,
		ITimesheetRepository timesheetRepository,
		ISystemConfigRepository systemConfigRepository,
		IAuditService auditService)
	{
		_projectRepository = projectRepository;
		_userRepository = userRepository;
		_timesheetRepository = timesheetRepository;
		_systemConfigRepository = systemConfigRepository;
		_auditService = auditService;
	}

	public async Task<ProjectCreatedDto> CreateProjectAsync(
		CreateProjectDto dto,
		long actorUserId,
		CancellationToken cancellationToken = default)
	{
		ValidateProjectDates(dto.StartDate, dto.EndDate, allowPastStartDate: false);
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
		await _auditService.LogAsync(
			actorUserId,
			"CREATE",
			"PROJECTS",
			project.Id,
			"Project created",
			newValues: $"{{\"projectName\":\"{project.ProjectName}\",\"managerUserId\":{project.ManagerUserId}}}",
			cancellationToken: cancellationToken);

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
		long actorUserId,
		CancellationToken cancellationToken = default)
	{
		var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

		if (project == null || !project.IsActive)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}

		await ValidateManagerAsync(dto.ManagerUserId, cancellationToken);
		ValidateProjectDates(dto.StartDate, dto.EndDate, allowPastStartDate: dto.StartDate == project.StartDate);
		ValidateMilestonesWithinProjectDates(project.Milestones, dto.StartDate, dto.EndDate);

		var oldValues = $"{{\"projectName\":\"{project.ProjectName}\",\"status\":\"{project.ProjectStatus}\"}}";
		project.ProjectName = dto.ProjectName;
		project.Description = dto.Description;
		project.StartDate = dto.StartDate;
		project.EndDate = dto.EndDate;
		project.ProjectStatus = dto.ProjectStatus;
		project.TotalStoryPoints = dto.TotalStoryPoints;
		project.ManagerUserId = dto.ManagerUserId;
		project.UpdatedAt = DateTime.UtcNow;

		await _projectRepository.SaveChangesAsync(cancellationToken);
		await _auditService.LogAsync(
			actorUserId,
			"UPDATE",
			"PROJECTS",
			project.Id,
			"Project updated",
			oldValues: oldValues,
			newValues: $"{{\"projectName\":\"{project.ProjectName}\",\"status\":\"{project.ProjectStatus}\"}}",
			cancellationToken: cancellationToken);
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
		long actorUserId,
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
		await _auditService.LogAsync(
			actorUserId,
			"ADD_MILESTONE",
			"PROJECT_MILESTONES",
			milestone.Id,
			$"Milestone added to project {projectId}",
			newValues: $"{{\"projectId\":{projectId},\"milestoneTitle\":\"{milestone.MilestoneTitle}\"}}",
			cancellationToken: cancellationToken);
		return MapMilestone(milestone);
	}

	public async Task UpdateMilestoneStatusAsync(
		long projectId,
		long milestoneId,
		UpdateMilestoneStatusDto dto,
		long actorUserId,
		CancellationToken cancellationToken = default)
	{
		await EnsureProjectExistsAsync(projectId, cancellationToken);

		var milestone = await _projectRepository.GetMilestoneAsync(projectId, milestoneId, cancellationToken);

		if (milestone == null)
		{
			throw new NotFoundException($"Milestone {milestoneId} was not found for project {projectId}.");
		}

		var oldStatus = milestone.MilestoneStatus;
		milestone.MilestoneStatus = dto.MilestoneStatus;
		milestone.UpdatedAt = DateTime.UtcNow;
		milestone.CompletedAt = dto.MilestoneStatus == MilestoneStatuses.Done
			? DateTime.UtcNow
			: null;

		await _projectRepository.SaveChangesAsync(cancellationToken);
		await _auditService.LogAsync(
			actorUserId,
			"UPDATE_MILESTONE_STATUS",
			"PROJECT_MILESTONES",
			milestone.Id,
			$"Milestone status updated for project {projectId}",
			oldValues: $"{{\"status\":\"{oldStatus}\"}}",
			newValues: $"{{\"status\":\"{milestone.MilestoneStatus}\"}}",
			cancellationToken: cancellationToken);
	}

	public async Task<IReadOnlyList<ManagerProjectListItemDto>> GetMyProjectsAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var projects = await _projectRepository.GetByManagerUserIdAsync(managerUserId, cancellationToken);

		return projects.Select(project => new ManagerProjectListItemDto
		{
			Id = project.Id,
			ProjectName = project.ProjectName,
			EndDate = project.EndDate,
			HealthStatus = project.HealthStatus,
			ProjectStatus = project.ProjectStatus
		}).ToList();
	}

	public async Task<ManagerProjectDetailDto> GetProjectDetailAsync(
		long projectId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var project = await _projectRepository.GetDetailByIdAsync(projectId, cancellationToken);

		if (project == null)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}

		if (project.ManagerUserId != managerUserId)
		{
			throw new ValidationException("You can only view your own projects.");
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var lastWeekStart = WeekHelper.GetLastCompletedWeekStart(today);
		var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);

		var activeAllocations = project.ProjectAllocations
			.Where(allocation =>
				allocation.AllocationStatus == "ACTIVE"
				&& allocation.AllocationStartDate <= today
				&& allocation.AllocationEndDate >= today)
			.ToList();

		var lastWeekHours = new List<(string EmployeeName, decimal HoursLogged, decimal ExpectedHours)>();
		foreach (var allocation in activeAllocations)
		{
			var expectedHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
			var loggedHours = await _timesheetRepository.GetLoggedHoursForProjectResourceProfileWeekAsync(
				project.Id,
				allocation.ResourceProfileId,
				lastWeekStart,
				cancellationToken);

			lastWeekHours.Add((allocation.ResourceProfile.User.FullName, loggedHours, expectedHours));
		}

		var riskFlags = ProjectHealthEvaluator.EvaluateRiskFlags(project, activeAllocations, lastWeekHours);

		return new ManagerProjectDetailDto
		{
			Id = project.Id,
			ProjectName = project.ProjectName,
			HealthStatus = project.HealthStatus,
			RiskFlags = riskFlags,
			Milestones = project.Milestones
				.OrderBy(milestone => milestone.SortOrder)
				.Select(milestone => new ManagerMilestoneDto
				{
					Id = milestone.Id,
					SortOrder = milestone.SortOrder,
					MilestoneTitle = milestone.MilestoneTitle,
					DueDate = milestone.DueDate,
					MilestoneStatus = milestone.MilestoneStatus,
					IsOverdue = milestone.DueDate < today && milestone.MilestoneStatus != MilestoneStatuses.Done
				}).ToList(),
			AllocatedResources = activeAllocations
				.OrderBy(allocation => allocation.ResourceProfile.User.FullName)
				.Select(allocation => new ProjectResourceDto
				{
					EmployeeName = allocation.ResourceProfile.User.FullName,
					AllocationPercentage = allocation.AllocationPercentage,
					AllocationStartDate = allocation.AllocationStartDate,
					AllocationEndDate = allocation.AllocationEndDate
				}).ToList()
		};
	}

	public async Task EvaluateAllProjectsHealthAsync(CancellationToken cancellationToken = default)
	{
		var projects = await _projectRepository.GetActiveProjectsWithMilestonesAsync(cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var lastWeekStart = WeekHelper.GetLastCompletedWeekStart(today);
		var lastWeekEnd = WeekHelper.GetWeekEnd(lastWeekStart);
		var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);

		foreach (var project in projects)
		{
			var expectedHours = HealthStatusCalculator.CalculateExpectedWeeklyHours(
				project.ProjectAllocations,
				lastWeekStart,
				lastWeekEnd,
				maxWeeklyHours);
			var loggedHours = await _timesheetRepository.GetLoggedHoursForProjectWeekAsync(
				project.Id,
				lastWeekStart,
				cancellationToken);
			var flagCount = HealthStatusCalculator.CountHealthFlags(
				project,
				loggedHours,
				expectedHours,
				today);
			var healthStatus = HealthStatusCalculator.FromFlagCount(flagCount);

			await _projectRepository.UpdateHealthStatusAsync(project.Id, healthStatus, cancellationToken);
		}
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

	private static void ValidateProjectDates(DateOnly startDate, DateOnly endDate, bool allowPastStartDate)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		if (!allowPastStartDate && startDate < today)
		{
			throw new ValidationException("Project start date cannot be in the past.");
		}

		if (endDate < startDate)
		{
			throw new ValidationException("Project end date cannot be before start date.");
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
			Description = project.Description,
			ManagerName = project.ManagerUser.FullName,
			ManagerUserId = project.ManagerUserId,
			StartDate = project.StartDate,
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
