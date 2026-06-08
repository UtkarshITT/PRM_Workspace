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
			new UserRepository(_context));
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
