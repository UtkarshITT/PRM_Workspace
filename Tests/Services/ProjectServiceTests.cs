using FluentAssertions;
using Moq;
using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class ProjectServiceTests
{
	private readonly Mock<IProjectRepository> _projectRepository = new();
	private readonly Mock<IUserRepository> _userRepository = new();
	private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
	private readonly Mock<ISystemConfigRepository> _systemConfigRepository = new();
	private readonly Mock<IAuditService> _auditService = new();
	private readonly ProjectService _projectService;

	public ProjectServiceTests()
	{
		_projectService = new ProjectService(
			_projectRepository.Object,
			_userRepository.Object,
			_timesheetRepository.Object,
			_systemConfigRepository.Object,
			_auditService.Object);
		_systemConfigRepository
			.Setup(repository => repository.GetValueByKeyAsync(SystemConfigKeys.MaxWeeklyHours, It.IsAny<CancellationToken>()))
			.ReturnsAsync("40");
	}

	[Fact]
	public async Task CreateProjectAsync_WithValidInput_CreatesProjectWithCode()
	{
		const long managerId = 10;
		ArrangeManager(managerId);
		_projectRepository
			.Setup(repository => repository.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
			.Callback<Project, CancellationToken>((project, _) => project.Id = 101)
			.ReturnsAsync((Project project, CancellationToken _) => project);
		var startDate = DateOnly.FromDateTime(DateTime.Today);

		var result = await _projectService.CreateProjectAsync(new CreateProjectDto
		{
			ProjectName = "Alpha Portal",
			Description = "Customer portal",
			StartDate = startDate,
			EndDate = startDate.AddMonths(6),
			ProjectStatus = ProjectStatuses.Active,
			ManagerUserId = managerId,
			TotalStoryPoints = 120
		}, actorUserId: 1);

		result.ProjectId.Should().BeGreaterThan(0);
		result.ProjectCode.Should().Be($"PRJ-{result.ProjectId:D6}");
		_projectRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddMilestoneAsync_WithDueDateOutsideProjectRange_ThrowsValidation()
	{
		var project = ArrangeProject();

		var act = () => _projectService.AddMilestoneAsync(project.Id, new CreateMilestoneDto
		{
			MilestoneTitle = "Late Milestone",
			DueDate = new DateOnly(2027, 1, 1),
			StoryPoints = 10,
			SortOrder = 1
		}, actorUserId: 1);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*between*");
	}

	[Fact]
	public async Task GetMyProjectsAsync_ReturnsOnlyManagerOwnProjects()
	{
		const long managerId = 10;
		_projectRepository
			.Setup(repository => repository.GetByManagerUserIdAsync(managerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				CreateProject(1, managerId, "Alpha Portal"),
				CreateProject(2, managerId, "Beta CRM")
			]);

		var result = await _projectService.GetMyProjectsAsync(managerId);

		result.Should().HaveCount(2);
		result.Select(project => project.ProjectName).Should().BeEquivalentTo(["Alpha Portal", "Beta CRM"]);
	}

	[Fact]
	public async Task GetProjectDetailAsync_WhenNotProjectManager_ThrowsValidation()
	{
		const long managerId = 10;
		const long otherManagerId = 20;
		var project = CreateProject(1, managerId, "Alpha Portal");
		_projectRepository
			.Setup(repository => repository.GetDetailByIdAsync(project.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(project);

		var act = () => _projectService.GetProjectDetailAsync(project.Id, otherManagerId);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*your own projects*");
	}

	[Fact]
	public async Task EvaluateAllProjectsHealthAsync_WithOverdueMilestone_SetsAmber()
	{
		var project = CreateHealthProject(
			dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
			milestoneStatus: MilestoneStatuses.InProgress);
		_projectRepository
			.Setup(repository => repository.GetActiveProjectsWithMilestonesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([project]);
		_timesheetRepository
			.Setup(repository => repository.GetLoggedHoursForProjectWeekAsync(project.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(40);

		await _projectService.EvaluateAllProjectsHealthAsync();

		_projectRepository.Verify(
			repository => repository.UpdateHealthStatusAsync(project.Id, "AMBER", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task EvaluateAllProjectsHealthAsync_WithMultipleFlags_SetsRed()
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var project = CreateHealthProject(
			dueDate: today.AddDays(-3),
			milestoneStatus: MilestoneStatuses.InProgress,
			endDate: today.AddDays(14));
		_projectRepository
			.Setup(repository => repository.GetActiveProjectsWithMilestonesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([project]);
		_timesheetRepository
			.Setup(repository => repository.GetLoggedHoursForProjectWeekAsync(project.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(40);

		await _projectService.EvaluateAllProjectsHealthAsync();

		_projectRepository.Verify(
			repository => repository.UpdateHealthStatusAsync(project.Id, "RED", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AddMilestoneAsync_WithValidDueDate_AddsMilestone()
	{
		var project = ArrangeProject();
		_projectRepository
			.Setup(repository => repository.AddMilestoneAsync(It.IsAny<ProjectMilestone>(), It.IsAny<CancellationToken>()))
			.Callback<ProjectMilestone, CancellationToken>((milestone, _) => milestone.Id = 11)
			.ReturnsAsync((ProjectMilestone milestone, CancellationToken _) => milestone);

		var result = await _projectService.AddMilestoneAsync(project.Id, new CreateMilestoneDto
		{
			MilestoneTitle = "Backend API",
			DueDate = new DateOnly(2026, 4, 15),
			StoryPoints = 40,
			SortOrder = 1
		}, actorUserId: 1);

		result.MilestoneTitle.Should().Be("Backend API");
		result.MilestoneStatus.Should().Be(MilestoneStatuses.NotStarted);
	}

	private void ArrangeManager(long managerId)
	{
		_userRepository
			.Setup(repository => repository.GetByIdAsync(managerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new User
			{
				Id = managerId,
				Role = Roles.Manager,
				IsActive = true
			});
	}

	private Project ArrangeProject(long projectId = 1, long managerId = 10)
	{
		var project = CreateProject(projectId, managerId, "Alpha Portal");
		project.StartDate = new DateOnly(2026, 1, 1);
		project.EndDate = new DateOnly(2026, 6, 30);
		project.TotalStoryPoints = 120;
		_projectRepository
			.Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(project);
		return project;
	}

	private static Project CreateProject(long projectId, long managerId, string projectName)
	{
		return new Project
		{
			Id = projectId,
			ProjectCode = $"PRJ-{projectId:D6}",
			ProjectName = projectName,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 6, 30),
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 100,
			ManagerUserId = managerId,
			HealthStatus = "GREEN",
			IsActive = true
		};
	}

	private static Project CreateHealthProject(
		DateOnly dueDate,
		string milestoneStatus,
		DateOnly? endDate = null)
	{
		var now = DateTime.UtcNow;
		var projectEnd = endDate ?? new DateOnly(2026, 12, 31);
		return new Project
		{
			Id = 501,
			ProjectCode = "PRJ-HEALTH",
			ProjectName = "Health Test Project",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = projectEnd,
			ProjectStatus = ProjectStatuses.Active,
			HealthStatus = "GREEN",
			TotalStoryPoints = 100,
			ManagerUserId = 10,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now,
			Milestones =
			[
				new ProjectMilestone
				{
					ProjectId = 501,
					MilestoneTitle = "Backend API",
					DueDate = dueDate,
					StoryPoints = 40,
					SortOrder = 1,
					MilestoneStatus = milestoneStatus,
					CreatedAt = now,
					UpdatedAt = now
				}
			],
			ProjectAllocations =
			[
				new ProjectAllocation
				{
					ResourceProfileId = 20,
					AllocationPercentage = 100,
					AllocationStatus = "ACTIVE",
					AllocationStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
					AllocationEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)
				}
			]
		};
	}
}
