using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Ai;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Ai;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class AiIntegrationServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly Mock<ILlmClient> _llmClientMock;
	private readonly Mock<ILlmClientFactory> _llmClientFactoryMock;
	private readonly AiIntegrationService _service;

	public AiIntegrationServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_llmClientMock = new Mock<ILlmClient>();
		_llmClientFactoryMock = new Mock<ILlmClientFactory>();
		_llmClientFactoryMock.Setup(factory => factory.Create(It.IsAny<string>())).Returns(_llmClientMock.Object);

		_service = new AiIntegrationService(
			new ResourceProfileRepository(_context),
			new ProjectRepository(_context),
			new TimesheetRepository(_context),
			new SystemConfigRepository(_context),
			new AiRequestLogRepository(_context),
			new SkillRepository(_context),
			_llmClientFactoryMock.Object,
			NullLogger<AiIntegrationService>.Instance);
	}

	[Fact]
	public async Task GetSkillMatchAsync_WithNoActiveEmployees_ReturnsNoCandidatesMessage()
	{
		var managerId = await SeedManagerWithoutTeamAsync();

		var result = await _service.GetSkillMatchAsync("Need a JavaScript developer", managerId);

		result.AiGenerated.Should().BeFalse();
		result.Candidates.Should().BeEmpty();
		result.Message.Should().Contain("No active employees");
		result.Disclaimer.Should().BeEmpty();
		_llmClientMock.Verify(
			client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetSkillMatchAsync_WithAvailableEmployeeOutsideManagersTeam_ReturnsOrganizationWideMatch()
	{
		await SeedManagerWithTeamAsync(utilizationPercent: 0);
		var managerId = await SeedManagerWithoutTeamAsync();

		var result = await _service.GetSkillMatchAsync("Java backend", managerId);

		result.Candidates.Should().NotBeEmpty();
		result.Candidates[0].FullName.Should().Be("Java Employee");
		_llmClientMock.Verify(
			client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetSkillMatchAsync_ForAllocationRecommendation_DoesNotCreateAllocation()
	{
		await SeedManagerWithTeamAsync(utilizationPercent: 0);
		var managerId = await SeedManagerWithoutTeamAsync();
		var allocationsBefore = await _context.ProjectAllocations.CountAsync();

		var result = await _service.GetSkillMatchAsync("Java backend", managerId);

		result.Candidates.Should().NotBeEmpty();
		_context.ProjectAllocations.Count().Should().Be(allocationsBefore);
	}

	[Fact]
	public async Task GetSkillMatchAsync_WithFullyUtilizedOrganization_ReturnsNoEligibleCandidates()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 100);

		var result = await _service.GetSkillMatchAsync("Need a Java developer", managerId);

		result.AiGenerated.Should().BeFalse();
		result.Candidates.Should().BeEmpty();
		result.Message.Should().Contain("No eligible candidates");
		result.Disclaimer.Should().BeEmpty();
		result.GapAnalysis.Should().NotBeEmpty();
		_llmClientMock.Verify(
			client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetSkillMatchAsync_WhenLlmFails_ReturnsFallbackMessage()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 0);
		await SeedLlmConfigAsync();

		_llmClientMock
			.Setup(client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Service unavailable"));

		var result = await _service.GetSkillMatchAsync("Need React developer", managerId);

		result.AiGenerated.Should().BeFalse();
		result.Message.Should().Contain("AI service unavailable");
		result.Disclaimer.Should().BeEmpty();
	}

	[Fact]
	public async Task GetSkillMatchAsync_FiltersHallucinatedEmployeeIds()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 0);
		var employeeId = await _context.ResourceProfiles.Select(profile => profile.Id).FirstAsync();
		await SeedLlmConfigAsync();

		_llmClientMock
			.Setup(client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("""
				{
				  "candidates": [
				    { "employeeId": 99999, "rank": 1, "matchScore": 95, "reason": "Fake" },
				    { "employeeId": REPLACEME, "rank": 2, "matchScore": 80, "reason": "Real" }
				  ]
				}
				""".Replace("REPLACEME", employeeId.ToString()));

		var result = await _service.GetSkillMatchAsync("Need backend developer", managerId);

		result.AiGenerated.Should().BeTrue();
		result.Candidates.Should().HaveCount(1);
		result.Candidates[0].EmployeeId.Should().Be(employeeId);
	}

	[Fact]
	public async Task GetSkillMatchAsync_WithoutApiKey_UsesDeterministicMatching()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 0);

		var result = await _service.GetSkillMatchAsync("Java backend", managerId);

		result.Candidates.Should().NotBeEmpty();
		_llmClientMock.Verify(
			client => client.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetProjectRiskSummaryAsync_ForOtherManagersProject_ThrowsValidation()
	{
		var (_, managerId, projectId) = await SeedProjectAsync();

		var act = () => _service.GetProjectRiskSummaryAsync(projectId, managerId + 999);

		await act.Should().ThrowAsync<ValidationException>();
	}

	[Fact]
	public async Task BuildTeamAsync_WithNoEligibleCandidates_ReturnsMessage()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 100);

		var result = await _service.BuildTeamAsync(new TeamBuilderRequestDto
		{
			Roles =
			[
				new TeamRoleRequirementDto
				{
					RoleTitle = "Backend Dev",
					AllocationPercent = 100,
					Headcount = 1
				}
			]
		}, managerId);

		result.AiGenerated.Should().BeFalse();
		result.Message.Should().Contain("No eligible candidates");
		result.GapAnalysis.Should().NotBeEmpty();
	}

	[Fact]
	public async Task BuildTeamAsync_WithPrompt_UsesOnlyFullyAvailableCandidates()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 0);
		var employeeId = await _context.ResourceProfiles.Select(profile => profile.Id).FirstAsync();
		await SeedLlmConfigAsync();

		_llmClientMock
			.Setup(client => client.GenerateResponseAsync(
				It.Is<string>(prompt => prompt.Contains("4 developers", StringComparison.OrdinalIgnoreCase)
				                       && prompt.Contains("fullyAvailableCandidates", StringComparison.OrdinalIgnoreCase)),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync("""
				{
				  "summary": "Use the fully available developer.",
				  "gapAnalysis": ["Some requested skills may need training."],
				  "roles": [
				    {
				      "roleTitle": "Developer",
				      "headcount": 4,
				      "candidates": [
				        { "employeeId": REPLACEME, "rank": 1, "matchScore": 88, "reason": "Fully available and has Java skills" },
				        { "employeeId": 99999, "rank": 2, "matchScore": 80, "reason": "Fake candidate" }
				      ]
				    }
				  ]
				}
				""".Replace("REPLACEME", employeeId.ToString()));

		var result = await _service.BuildTeamAsync(new TeamBuilderRequestDto
		{
			Prompt = "I need 4 developers for Python, 1 QA, and 1 DevOps"
		}, managerId);

		result.AiGenerated.Should().BeTrue();
		result.Summary.Should().Contain("fully available");
		result.GapAnalysis.Should().ContainSingle();
		result.Roles.Should().ContainSingle();
		result.Roles[0].RoleTitle.Should().Be("Developer");
		result.Roles[0].Matches.Should().ContainSingle();
		result.Roles[0].Matches[0].EmployeeId.Should().Be(employeeId);
		result.Roles[0].Matches[0].AvailabilityPercent.Should().Be(100);
	}

	[Fact]
	public async Task BuildTeamAsync_WithoutApiKey_GroupsFallbackByRoleAndExplainsGaps()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 100);
		await SeedAvailableEmployeeAsync("qa.available", "QA Available", "QA", "Selenium", managerId: null);

		var result = await _service.BuildTeamAsync(new TeamBuilderRequestDto
		{
			Prompt = "Need Java guy and a QA engineer for a new project"
		}, managerId);

		result.AiGenerated.Should().BeFalse();
		result.Roles.Should().Contain(role => role.RoleTitle == "Java Resource");
		result.Roles.Should().Contain(role => role.RoleTitle == "QA Engineer");
		result.Roles.Single(role => role.RoleTitle == "Java Resource").Matches.Should().BeEmpty();
		result.Roles.Single(role => role.RoleTitle == "QA Engineer").Matches.Should().ContainSingle();
		result.Roles.Single(role => role.RoleTitle == "QA Engineer").Matches[0].Reason.Should().Contain("Best 100% available fit");
		result.GapAnalysis.Should().Contain(gap => gap.Contains("fully allocated", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task BuildTeamAsync_WithoutApiKey_DoesNotMatchJavaInsideJavaScript()
	{
		var managerId = await SeedManagerWithTeamAsync(utilizationPercent: 100);
		var javascriptEmployeeId = await SeedAvailableEmployeeAsync(
			"js.available",
			"JavaScript Available",
			"Frontend",
			"JavaScript",
			managerId: null);

		var result = await _service.BuildTeamAsync(new TeamBuilderRequestDto
		{
			Prompt = "Need Java and JavaScript developers"
		}, managerId);

		var javaRole = result.Roles.Single(role => role.RoleTitle == "Java Resource");
		var javascriptRole = result.Roles.Single(role => role.RoleTitle == "JavaScript Resource");
		javaRole.Matches.Should().BeEmpty();
		javascriptRole.Matches.Should().ContainSingle();
		javascriptRole.Matches[0].EmployeeId.Should().Be(javascriptEmployeeId);
	}

	private async Task<long> SeedManagerWithTeamAsync(int utilizationPercent)
	{
		var now = DateTime.UtcNow;
		var managerUser = new User
		{
			Username = "mgr.ai",
			Email = "mgr.ai@test.com",
			FullName = "AI Manager",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Users.Add(managerUser);
		await _context.SaveChangesAsync();

		var employeeUser = new User
		{
			Username = "emp.ai",
			Email = "emp.ai@test.com",
			FullName = "Java Employee",
			PasswordHash = "hash",
			Role = Roles.Employee,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Users.Add(employeeUser);
		await _context.SaveChangesAsync();

		var skill = new Skill { SkillName = "Java", Category = "Backend", IsActive = true, CreatedAt = now };
		_context.Skills.Add(skill);

		var employee = new ResourceProfile
		{
			UserId = employeeUser.Id,
			ManagerId = managerUser.Id,
			ResourceProfileCode = "EMP-000101",
			Department = "Backend",
			EmploymentStatus = utilizationPercent > 0 ? "ALLOCATED" : "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.ResourceProfiles.Add(employee);
		await _context.SaveChangesAsync();

		_context.ResourceProfileSkills.Add(new ResourceProfileSkill
		{
			ResourceProfileId = employee.Id,
			SkillId = skill.Id,
			ProficiencyLevel = "Advanced",
			CreatedAt = now
		});

		if (utilizationPercent > 0)
		{
			var project = new Project
			{
				ProjectCode = "PRJ-000101",
				ProjectName = "Alpha",
				StartDate = new DateOnly(2026, 1, 1),
				EndDate = new DateOnly(2026, 12, 31),
				ProjectStatus = "ACTIVE",
				HealthStatus = "GREEN",
				ManagerUserId = managerUser.Id,
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			};
			_context.Projects.Add(project);
			await _context.SaveChangesAsync();

			_context.ProjectAllocations.Add(new ProjectAllocation
			{
				ResourceProfileId = employee.Id,
				ProjectId = project.Id,
				AllocationPercentage = utilizationPercent,
				AllocationStartDate = new DateOnly(2026, 1, 1),
				AllocationEndDate = new DateOnly(2026, 12, 31),
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = managerUser.Id,
				CreatedAt = now,
				UpdatedAt = now
			});
		}

		await _context.SaveChangesAsync();
		return managerUser.Id;
	}

	private async Task<long> SeedAvailableEmployeeAsync(
		string username,
		string fullName,
		string department,
		string skillName,
		long? managerId)
	{
		var now = DateTime.UtcNow;
		var employeeUser = new User
		{
			Username = username,
			Email = $"{username}@test.com",
			FullName = fullName,
			PasswordHash = "hash",
			Role = Roles.Employee,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Users.Add(employeeUser);
		await _context.SaveChangesAsync();

		var skill = await _context.Skills.FirstOrDefaultAsync(item => item.SkillName == skillName);
		if (skill == null)
		{
			skill = new Skill { SkillName = skillName, Category = department, IsActive = true, CreatedAt = now };
			_context.Skills.Add(skill);
			await _context.SaveChangesAsync();
		}

		var employee = new ResourceProfile
		{
			UserId = employeeUser.Id,
			ManagerId = managerId,
			ResourceProfileCode = $"EMP-{employeeUser.Id:D6}",
			Department = department,
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.ResourceProfiles.Add(employee);
		await _context.SaveChangesAsync();

		_context.ResourceProfileSkills.Add(new ResourceProfileSkill
		{
			ResourceProfileId = employee.Id,
			SkillId = skill.Id,
			ProficiencyLevel = "Advanced",
			CreatedAt = now
		});
		await _context.SaveChangesAsync();
		return employee.Id;
	}

	private async Task<long> SeedManagerWithoutTeamAsync()
	{
		var now = DateTime.UtcNow;
		var managerUser = new User
		{
			Username = "mgr.no-team",
			Email = "mgr.no-team@test.com",
			FullName = "No Team Manager",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.Add(managerUser);
		await _context.SaveChangesAsync();
		return managerUser.Id;
	}

	private async Task<(long ownerManagerId, long otherManagerId, long projectId)> SeedProjectAsync()
	{
		var now = DateTime.UtcNow;
		var owner = new User
		{
			Username = "owner",
			Email = "owner@test.com",
			FullName = "Owner",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		var other = new User
		{
			Username = "other",
			Email = "other@test.com",
			FullName = "Other",
			PasswordHash = "hash",
			Role = Roles.Manager,
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Users.AddRange(owner, other);
		await _context.SaveChangesAsync();

		var project = new Project
		{
			ProjectCode = "PRJ-000201",
			ProjectName = "Beta",
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31),
			ProjectStatus = "ACTIVE",
			HealthStatus = "AMBER",
			ManagerUserId = owner.Id,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Projects.Add(project);
		await _context.SaveChangesAsync();
		return (owner.Id, other.Id, project.Id);
	}

	private async Task SeedLlmConfigAsync()
	{
		var now = DateTime.UtcNow;
		_context.SystemConfigurations.AddRange(
			new SystemConfiguration
			{
				ConfigKey = SystemConfigKeys.LlmProvider,
				ConfigValue = "Gemini",
				UpdatedAt = now
			},
			new SystemConfiguration
			{
				ConfigKey = SystemConfigKeys.LlmApiKey,
				ConfigValue = "test-key",
				UpdatedAt = now
			});
		await _context.SaveChangesAsync();
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
