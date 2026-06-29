using FluentAssertions;
using Moq;
using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Allocations;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class AllocationServiceTests
{
	private readonly Mock<IAllocationRepository> _allocationRepository = new();
	private readonly Mock<IResourceProfileRepository> _resourceProfileRepository = new();
	private readonly Mock<IProjectRepository> _projectRepository = new();
	private readonly Mock<ISystemConfigRepository> _systemConfigRepository = new();
	private readonly Mock<IAuditService> _auditService = new();
	private readonly AllocationService _allocationService;

	public AllocationServiceTests()
	{
		_allocationService = new AllocationService(
			_allocationRepository.Object,
			_resourceProfileRepository.Object,
			_projectRepository.Object,
			_systemConfigRepository.Object,
			_auditService.Object);
	}

	[Fact]
	public async Task CreateAllocationAsync_WithOverlappingUtilization_ThrowsOverAllocation()
	{
		var (managerId, employeeId, projectId, resourceProfile, _, startDate) = ArrangeCreateAllocation(existingUtilization: 60);

		var act = () => _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = startDate,
			AllocationEndDate = startDate.AddMonths(3)
		}, managerId);

		await act.Should().ThrowAsync<OverAllocationException>();
		resourceProfile.EmploymentStatus.Should().Be("BENCH");
		_allocationRepository.Verify(
			repository => repository.AddAsync(It.IsAny<ProjectAllocation>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CreateAllocationAsync_WithValidInput_CreatesAllocation()
	{
		var (managerId, employeeId, projectId, resourceProfile, _, startDate) = ArrangeCreateAllocation(existingUtilization: 0);
		ProjectAllocation? savedAllocation = null;
		_allocationRepository
			.Setup(repository => repository.AddAsync(It.IsAny<ProjectAllocation>(), It.IsAny<CancellationToken>()))
			.Callback<ProjectAllocation, CancellationToken>((allocation, _) =>
			{
				allocation.Id = 100;
				savedAllocation = allocation;
			})
			.Returns(Task.CompletedTask);

		var result = await _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 50,
			AllocationStartDate = startDate,
			AllocationEndDate = startDate.AddMonths(3)
		}, managerId);

		result.AllocationStatus.Should().Be("ACTIVE");
		result.EmploymentStatus.Should().Be("ALLOCATED");
		result.AllocationId.Should().Be(100);
		savedAllocation.Should().NotBeNull();
		savedAllocation!.ResourceProfileId.Should().Be(employeeId);
		resourceProfile.EmploymentStatus.Should().Be("ALLOCATED");
		_allocationRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task CreateAllocationAsync_WhenAlreadyAllocatedToProject_ThrowsValidation()
	{
		var (managerId, employeeId, projectId, _, _, startDate) = ArrangeCreateAllocation(
			existingUtilization: 50,
			existingProjectId: 1);

		var act = () => _allocationService.CreateAllocationAsync(new CreateAllocationDto
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = 25,
			AllocationStartDate = startDate.AddDays(7),
			AllocationEndDate = startDate.AddMonths(2)
		}, managerId);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*already has an active allocation for this project*");
		_allocationRepository.Verify(
			repository => repository.AddAsync(It.IsAny<ProjectAllocation>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task EndAllocationAsync_WhenLastAllocation_RevertsEmployeeToBench()
	{
		var (managerId, allocation) = ArrangeActiveAllocation();

		await _allocationService.EndAllocationAsync(allocation.Id, managerId);

		allocation.ResourceProfile.EmploymentStatus.Should().Be("BENCH");
		allocation.AllocationStatus.Should().Be("ENDED");
		_allocationRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_auditService.Verify(service => service.LogAsync(
			managerId,
			"END",
			"PROJECT_ALLOCATIONS",
			allocation.Id,
			It.IsAny<string?>(),
			It.IsAny<string?>(),
			It.IsAny<string?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task EndAllocationAsync_WhenNotProjectManager_ThrowsValidation()
	{
		var (_, allocation) = ArrangeActiveAllocation();
		var otherManagerId = 999;

		var act = () => _allocationService.EndAllocationAsync(allocation.Id, otherManagerId);
		await act.Should().ThrowAsync<ValidationException>();
		_allocationRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	private (long ManagerId, long EmployeeId, long ProjectId, ResourceProfile ResourceProfile, Project Project, DateOnly StartDate)
		ArrangeCreateAllocation(decimal existingUtilization, long? existingProjectId = null)
	{
		const long managerId = 10;
		const long employeeId = 20;
		const long projectId = 1;
		var startDate = DateOnly.FromDateTime(DateTime.Today);
		var resourceProfile = new ResourceProfile
		{
			Id = employeeId,
			ManagerId = managerId,
			ResourceProfileCode = "EMP-000002",
			EmploymentStatus = "BENCH",
			IsActive = true,
			User = new User
			{
				Id = 30,
				FullName = "Employee",
				Role = Roles.Employee
			}
		};
		var project = new Project
		{
			Id = projectId,
			ProjectCode = "PRJ-000001",
			ProjectName = "Alpha Portal",
			StartDate = startDate,
			EndDate = startDate.AddMonths(6),
			ProjectStatus = ProjectStatuses.Active,
			TotalStoryPoints = 100,
			ManagerUserId = managerId,
			IsActive = true
		};

		var activeAllocations = new List<ProjectAllocation>();

		if (existingUtilization > 0)
		{
			activeAllocations.Add(new ProjectAllocation
			{
				Id = 55,
				ResourceProfileId = employeeId,
				ProjectId = existingProjectId ?? 999,
				AllocationPercentage = existingUtilization,
				AllocationStartDate = startDate,
				AllocationEndDate = startDate.AddMonths(3),
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = managerId
			});
		}

		_resourceProfileRepository
			.Setup(repository => repository.GetTeamMemberAsync(employeeId, managerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(resourceProfile);
		_projectRepository
			.Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(project);
		_allocationRepository
			.Setup(repository => repository.GetActiveByResourceProfileIdAsync(employeeId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(activeAllocations);
		_allocationRepository
			.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		_systemConfigRepository
			.Setup(repository => repository.GetValueByKeyAsync(SystemConfigKeys.MaxWeeklyHours, It.IsAny<CancellationToken>()))
			.ReturnsAsync("40");

		return (managerId, employeeId, projectId, resourceProfile, project, startDate);
	}

	private (long ManagerId, ProjectAllocation Allocation) ArrangeActiveAllocation()
	{
		const long managerId = 10;
		var allocation = new ProjectAllocation
		{
			Id = 77,
			ResourceProfileId = 20,
			ProjectId = 1,
			AllocationPercentage = 50,
			AllocationStartDate = DateOnly.FromDateTime(DateTime.Today),
			AllocationEndDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(3),
			AllocationStatus = "ACTIVE",
			ResourceProfile = new ResourceProfile
			{
				Id = 20,
				EmploymentStatus = "ALLOCATED"
			},
			Project = new Project
			{
				Id = 1,
				ManagerUserId = managerId
			}
		};

		_allocationRepository
			.Setup(repository => repository.GetByIdWithDetailsAsync(allocation.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(allocation);
		_allocationRepository
			.Setup(repository => repository.GetActiveByResourceProfileIdAsync(allocation.ResourceProfileId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([allocation]);
		_allocationRepository
			.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		_auditService
			.Setup(service => service.LogAsync(
				It.IsAny<long>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<long>(),
				It.IsAny<string?>(),
				It.IsAny<string?>(),
				It.IsAny<string?>(),
				It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		return (managerId, allocation);
	}
}
