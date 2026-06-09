using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class ProjectServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly ProjectService _projectService;

	public ProjectServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_projectService = new ProjectService(
			new ProjectRepository(_context),
			new UserRepository(_context),
			new TimesheetRepository(_context),
			new SystemConfigRepository(_context));
	}

	[Fact]
	public async Task CreateProjectAsync_WithValidInput_CreatesProjectWithCode()
	{
		var managerId = await SeedManagerAsync();

		var result = await _projectService.CreateProjectAsync(new CreateProjectDto
		{
			ProjectName = "Alpha Portal",
			Description = "Customer portal",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 6, 30),
			ProjectStatus = ProjectStatuses.Active,
			ManagerUserId = managerId,
			TotalStoryPoints = 120
		});

		result.ProjectId.Should().BeGreaterThan(0);
		result.ProjectCode.Should().Be($"PRJ-{result.ProjectId:D6}");
	}

	[Fact]
	public async Task AddMilestoneAsync_WithDueDateOutsideProjectRange_ThrowsValidation()
	{
		var projectId = await SeedProjectAsync();

		var act = () => _projectService.AddMilestoneAsync(projectId, new CreateMilestoneDto
		{
			MilestoneTitle = "Late Milestone",
			DueDate = new DateOnly(2027, 1, 1),
			StoryPoints = 10,
			SortOrder = 1
		});

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*between*");
	}

	[Fact]
	public async Task GetMyProjectsAsync_ReturnsOnlyManagerOwnProjects()
	{
		var (managerId, otherManagerId) = await SeedTwoManagersAsync();
		await SeedProjectForManagerAsync(managerId, "Alpha Portal");
		await SeedProjectForManagerAsync(managerId, "Beta CRM");
		await SeedProjectForManagerAsync(otherManagerId, "Other Project");

		var result = await _projectService.GetMyProjectsAsync(managerId);

		result.Should().HaveCount(2);
		result.Select(project => project.ProjectName).Should().BeEquivalentTo(["Alpha Portal", "Beta CRM"]);
	}

	[Fact]
	public async Task GetProjectDetailAsync_WhenNotProjectManager_ThrowsValidation()
	{
		var (managerId, otherManagerId) = await SeedTwoManagersAsync();
		var projectId = await SeedProjectForManagerAsync(managerId, "Alpha Portal");

		var act = () => _projectService.GetProjectDetailAsync(projectId, otherManagerId);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*your own projects*");
	}

	[Fact]
	public async Task AddMilestoneAsync_WithValidDueDate_AddsMilestone()
	{
		var projectId = await SeedProjectAsync();

		var result = await _projectService.AddMilestoneAsync(projectId, new CreateMilestoneDto
		{
			MilestoneTitle = "Backend API",
			DueDate = new DateOnly(2026, 4, 15),
			StoryPoints = 40,
			SortOrder = 1
		});

		result.MilestoneTitle.Should().Be("Backend API");
		result.MilestoneStatus.Should().Be(MilestoneStatuses.NotStarted);
	}

	private async Task<long> SeedManagerAsync()
	{
		var now = DateTime.UtcNow;
		var manager = new User
		{
			Username = "manager",
			Email = "manager@techserve.com",
			FullName = "Manager",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.Add(manager);
		await _context.SaveChangesAsync();
		return manager.Id;
	}

	private async Task<(long ManagerId, long OtherManagerId)> SeedTwoManagersAsync()
	{
		var now = DateTime.UtcNow;
		var manager = new User
		{
			Username = "manager1",
			Email = "manager1@techserve.com",
			FullName = "Manager One",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		var otherManager = new User
		{
			Username = "manager2",
			Email = "manager2@techserve.com",
			FullName = "Manager Two",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.AddRange(manager, otherManager);
		await _context.SaveChangesAsync();
		return (manager.Id, otherManager.Id);
	}

	private async Task<long> SeedProjectForManagerAsync(long managerId, string projectName)
	{
		var now = DateTime.UtcNow;
		var project = new Project
		{
			ProjectCode = $"PRJ-{Guid.NewGuid():N}"[..10],
			ProjectName = projectName,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 6, 30),
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 100,
			ManagerUserId = managerId,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		await _context.SaveChangesAsync();
		return project.Id;
	}

	private async Task<long> SeedProjectAsync()
	{
		var managerId = await SeedManagerAsync();
		var now = DateTime.UtcNow;
		var project = new Project
		{
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha Portal",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 6, 30),
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 120,
			ManagerUserId = managerId,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Projects.Add(project);
		await _context.SaveChangesAsync();
		return project.Id;
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
