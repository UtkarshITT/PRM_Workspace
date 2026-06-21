using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Allocations;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IAllocationService
{
	Task<IReadOnlyList<AllocationListItemDto>> GetAllAllocationsAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default);
	Task<AllocationCreatedDto> CreateAllocationAsync(
		CreateAllocationDto dto,
		long managerUserId,
		CancellationToken cancellationToken = default);
	Task EndAllocationAsync(long allocationId, long managerUserId, CancellationToken cancellationToken = default);
	Task<EmployeeAllocationsResponseDto> GetMyAllocationsAsync(
		long employeeId,
		DateOnly? weekStart,
		CancellationToken cancellationToken = default);
}

public class AllocationService : IAllocationService
{
	private readonly PrmDbContext _context;
	private readonly IAllocationRepository _allocationRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IProjectRepository _projectRepository;

	public AllocationService(
		PrmDbContext context,
		IAllocationRepository allocationRepository,
		IResourceProfileRepository resourceProfileRepository,
		IProjectRepository projectRepository)
	{
		_context = context;
		_allocationRepository = allocationRepository;
		_resourceProfileRepository = resourceProfileRepository;
		_projectRepository = projectRepository;
	}

	public async Task<IReadOnlyList<AllocationListItemDto>> GetAllAllocationsAsync(
		long? employeeId,
		long? projectId,
		string? status,
		CancellationToken cancellationToken = default)
	{
		var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "ACTIVE" : status;
		var allocations = await _allocationRepository.GetAllAsync(employeeId, projectId, effectiveStatus, cancellationToken);

		return allocations.Select(allocation => new AllocationListItemDto
		{
			Id = allocation.Id,
			EmployeeName = allocation.ResourceProfile.User.FullName,
			ProjectName = allocation.Project.ProjectName,
			AllocationPercentage = allocation.AllocationPercentage,
			AllocationStartDate = allocation.AllocationStartDate,
			AllocationEndDate = allocation.AllocationEndDate,
			AllocationStatus = allocation.AllocationStatus
		}).ToList();
	}

	public async Task<AllocationCreatedDto> CreateAllocationAsync(
		CreateAllocationDto dto,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		ValidateAllocationDates(dto.AllocationStartDate, dto.AllocationEndDate);
		var resourceProfile = await _resourceProfileRepository.GetTeamMemberAsync(dto.EmployeeId, managerUserId, cancellationToken);

		if (resourceProfile == null)
		{
			throw new ValidationException("Employee is not on your team.");
		}

		var project = await _projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken);

		if (project == null || !project.IsActive)
		{
			throw new NotFoundException($"Project with ID {dto.ProjectId} was not found.");
		}

		if (project.ManagerUserId != managerUserId)
		{
			throw new ValidationException("You can only allocate resources to your own projects.");
		}

		if (project.ProjectStatus is not (ProjectStatuses.Active or ProjectStatuses.Planned))
		{
			throw new ValidationException("Allocations are only allowed on ACTIVE or PLANNED projects.");
		}

		if (dto.AllocationStartDate < project.StartDate || dto.AllocationEndDate > project.EndDate)
		{
			throw new ValidationException("Allocation dates must fall within the project date range.");
		}

		var activeAllocations = await _allocationRepository.GetActiveByResourceProfileIdAsync(dto.EmployeeId, cancellationToken);
		var overlappingUtilization = UtilizationCalculator.CalculateOverlappingUtilization(
			activeAllocations,
			dto.AllocationStartDate,
			dto.AllocationEndDate);

		if (overlappingUtilization + dto.AllocationPercentage > 100)
		{
			throw new OverAllocationException(resourceProfile.User.FullName, overlappingUtilization + dto.AllocationPercentage);
		}

		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var now = DateTime.UtcNow;
			var allocation = new ProjectAllocation
			{
				ResourceProfileId = dto.EmployeeId,
				ProjectId = dto.ProjectId,
				AllocationPercentage = dto.AllocationPercentage,
				AllocationStartDate = dto.AllocationStartDate,
				AllocationEndDate = dto.AllocationEndDate,
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = managerUserId,
				CreatedAt = now,
				UpdatedAt = now
			};

			_context.ProjectAllocations.Add(allocation);
			resourceProfile.EmploymentStatus = "ALLOCATED";
			resourceProfile.UpdatedAt = now;

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return new AllocationCreatedDto
			{
				AllocationId = allocation.Id,
				EmployeeId = dto.EmployeeId,
				ProjectId = dto.ProjectId,
				AllocationPercentage = dto.AllocationPercentage,
				AllocationStatus = allocation.AllocationStatus,
				EmploymentStatus = resourceProfile.EmploymentStatus
			};
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task EndAllocationAsync(long allocationId, long managerUserId, CancellationToken cancellationToken = default)
	{
		var allocation = await _allocationRepository.GetByIdWithDetailsAsync(allocationId, cancellationToken);

		if (allocation == null)
		{
			throw new NotFoundException($"Allocation with ID {allocationId} was not found.");
		}

		if (allocation.AllocationStatus != "ACTIVE")
		{
			throw new ValidationException("Allocation is already ended.");
		}

		if (allocation.Project.ManagerUserId != managerUserId)
		{
			throw new ValidationException("You can only end allocations on your own projects.");
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var now = DateTime.UtcNow;

		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			allocation.AllocationStatus = "ENDED";
			allocation.AllocationEndDate = today;
			allocation.UpdatedAt = now;

			var remainingActive = await _allocationRepository.GetActiveByResourceProfileIdAsync(allocation.ResourceProfileId, cancellationToken);
			var hasOtherActive = remainingActive.Any(item => item.Id != allocationId);

			if (!hasOtherActive)
			{
				allocation.ResourceProfile.EmploymentStatus = "BENCH";
				allocation.ResourceProfile.UpdatedAt = now;
			}

			_context.AuditLogs.Add(new AuditLog
			{
				ActorUserId = managerUserId,
				EntityName = "PROJECT_ALLOCATIONS",
				EntityId = allocationId,
				ActionType = "END",
				NewValues = $"{{\"resourceProfileId\":{allocation.ResourceProfileId},\"endDate\":\"{today:yyyy-MM-dd}\"}}",
				CreatedAt = now
			});

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task<EmployeeAllocationsResponseDto> GetMyAllocationsAsync(
		long employeeId,
		DateOnly? weekStart,
		CancellationToken cancellationToken = default)
	{
		var allocations = await _allocationRepository.GetByResourceProfileIdWithProjectsAsync(employeeId, cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var activeUtilization = UtilizationCalculator.CalculateCurrentUtilization(allocations, today);

		IEnumerable<ProjectAllocation> filtered = allocations.Where(allocation => allocation.AllocationStatus == "ACTIVE");

		if (weekStart.HasValue)
		{
			var weekEnd = WeekHelper.GetWeekEnd(weekStart.Value);
			filtered = filtered.Where(allocation =>
				UtilizationCalculator.PeriodsOverlap(
					allocation.AllocationStartDate,
					allocation.AllocationEndDate,
					weekStart.Value,
					weekEnd));
		}

		return new EmployeeAllocationsResponseDto
		{
			Allocations = filtered.Select(allocation => new EmployeeAllocationDto
			{
				Id = allocation.Id,
				ProjectId = allocation.ProjectId,
				ProjectName = allocation.Project.ProjectName,
				AllocationPercentage = allocation.AllocationPercentage,
				AllocationStartDate = allocation.AllocationStartDate,
				AllocationEndDate = allocation.AllocationEndDate,
				AllocationStatus = allocation.AllocationStatus
			}).ToList(),
			TotalActiveUtilizationPercent = activeUtilization
		};
	}

	private static void ValidateAllocationDates(DateOnly startDate, DateOnly endDate)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		if (startDate < today)
		{
			throw new ValidationException("Allocation start date cannot be in the past.");
		}

		if (endDate <= startDate)
		{
			throw new ValidationException("Allocation end date must be after start date.");
		}
	}
}
