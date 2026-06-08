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
}

public class AllocationService : IAllocationService
{
	private readonly PrmDbContext _context;
	private readonly IAllocationRepository _allocationRepository;
	private readonly IEmployeeRepository _employeeRepository;
	private readonly IProjectRepository _projectRepository;

	public AllocationService(
		PrmDbContext context,
		IAllocationRepository allocationRepository,
		IEmployeeRepository employeeRepository,
		IProjectRepository projectRepository)
	{
		_context = context;
		_allocationRepository = allocationRepository;
		_employeeRepository = employeeRepository;
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
			EmployeeName = allocation.Employee.User.FullName,
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
		var employee = await _employeeRepository.GetTeamMemberAsync(dto.EmployeeId, managerUserId, cancellationToken);

		if (employee == null)
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

		var activeAllocations = await _allocationRepository.GetActiveByEmployeeIdAsync(dto.EmployeeId, cancellationToken);
		var overlappingUtilization = UtilizationCalculator.CalculateOverlappingUtilization(
			activeAllocations,
			dto.AllocationStartDate,
			dto.AllocationEndDate);

		if (overlappingUtilization + dto.AllocationPercentage > 100)
		{
			throw new OverAllocationException(employee.User.FullName, overlappingUtilization + dto.AllocationPercentage);
		}

		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var now = DateTime.UtcNow;
			var allocation = new ProjectAllocation
			{
				EmployeeId = dto.EmployeeId,
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
			employee.EmploymentStatus = "ALLOCATED";
			employee.UpdatedAt = now;

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return new AllocationCreatedDto
			{
				AllocationId = allocation.Id,
				EmployeeId = dto.EmployeeId,
				ProjectId = dto.ProjectId,
				AllocationPercentage = dto.AllocationPercentage,
				AllocationStatus = allocation.AllocationStatus,
				EmploymentStatus = employee.EmploymentStatus
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

			var remainingActive = await _allocationRepository.GetActiveByEmployeeIdAsync(allocation.EmployeeId, cancellationToken);
			var hasOtherActive = remainingActive.Any(item => item.Id != allocationId);

			if (!hasOtherActive)
			{
				allocation.Employee.EmploymentStatus = "BENCH";
				allocation.Employee.UpdatedAt = now;
			}

			_context.AuditLogs.Add(new AuditLog
			{
				ActorUserId = managerUserId,
				EntityName = "PROJECT_ALLOCATIONS",
				EntityId = allocationId,
				ActionType = "END",
				NewValues = $"{{\"employeeId\":{allocation.EmployeeId},\"endDate\":\"{today:yyyy-MM-dd}\"}}",
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
}
