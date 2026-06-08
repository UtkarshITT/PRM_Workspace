using Microsoft.EntityFrameworkCore;
using PRM.Server.Constants;
using PRM.Server.Helpers;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Employees;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IEmployeeService
{
	Task<IReadOnlyList<EmployeeListItemDto>> GetAllEmployeesAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default);
	Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
	Task DeactivateEmployeeAsync(long employeeId, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<EmployeeSkillDto>> AddSkillAsync(long employeeId, AddSkillDto dto, CancellationToken cancellationToken = default);
	Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default);
	Task AssignManagerAsync(long employeeId, AssignManagerDto dto, CancellationToken cancellationToken = default);
	Task<TeamDashboardDto> GetTeamDashboardAsync(long managerUserId, CancellationToken cancellationToken = default);
	Task<TeamMemberDetailDto> GetTeamMemberDetailAsync(long employeeId, long managerUserId, CancellationToken cancellationToken = default);
}

public class EmployeeService : IEmployeeService
{
	private readonly PrmDbContext _context;
	private readonly IEmployeeRepository _employeeRepository;
	private readonly IUserRepository _userRepository;
	private readonly ISkillRepository _skillRepository;
	private readonly IAllocationRepository _allocationRepository;

	public EmployeeService(
		PrmDbContext context,
		IEmployeeRepository employeeRepository,
		IUserRepository userRepository,
		ISkillRepository skillRepository,
		IAllocationRepository allocationRepository)
	{
		_context = context;
		_employeeRepository = employeeRepository;
		_userRepository = userRepository;
		_skillRepository = skillRepository;
		_allocationRepository = allocationRepository;
	}

	public async Task<IReadOnlyList<EmployeeListItemDto>> GetAllEmployeesAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default)
	{
		var employees = await _employeeRepository.GetAllAsync(status, department, cancellationToken);

		return employees.Select(MapListItem).ToList();
	}

	public async Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

		if (employee == null || !employee.IsActive)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		employee.User.FullName = dto.FullName;
		employee.Department = dto.Department;
		employee.Designation = dto.Designation;
		employee.UpdatedAt = DateTime.UtcNow;
		employee.User.UpdatedAt = DateTime.UtcNow;

		await _employeeRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task DeactivateEmployeeAsync(long employeeId, long actorUserId, CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

		if (employee == null)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		if (!employee.IsActive)
		{
			throw new ValidationException("Employee is already inactive.");
		}

		var activeAllocations = await _allocationRepository.GetActiveByEmployeeIdAsync(employeeId, cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var now = DateTime.UtcNow;
		var endedAllocationIds = new List<long>();

		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			employee.IsActive = false;
			employee.EmploymentStatus = "BENCH";
			employee.UpdatedAt = now;
			employee.User.IsActive = false;
			employee.User.UpdatedAt = now;

			foreach (var allocation in activeAllocations)
			{
				allocation.AllocationStatus = "ENDED";
				allocation.AllocationEndDate = today;
				allocation.UpdatedAt = now;
				endedAllocationIds.Add(allocation.Id);
			}

			_context.AuditLogs.Add(new AuditLog
			{
				ActorUserId = actorUserId,
				EntityName = "EMPLOYEES",
				EntityId = employeeId,
				ActionType = "DEACTIVATE",
				NewValues = $"{{\"endedAllocationIds\":[{string.Join(",", endedAllocationIds)}]}}",
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

	public async Task<IReadOnlyList<EmployeeSkillDto>> AddSkillAsync(
		long employeeId,
		AddSkillDto dto,
		CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

		if (employee == null || !employee.IsActive)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		var skill = await _skillRepository.GetByNameAsync(dto.SkillName, cancellationToken);

		if (skill == null)
		{
			skill = await _skillRepository.AddAsync(new Skill
			{
				SkillName = dto.SkillName,
				Category = dto.Category,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			}, cancellationToken);
		}

		var existing = await _skillRepository.GetEmployeeSkillAsync(employeeId, skill.Id, cancellationToken);

		if (existing != null)
		{
			throw new ConflictException($"Employee already has skill '{dto.SkillName}'.");
		}

		await _skillRepository.AddEmployeeSkillAsync(new EmployeeSkill
		{
			EmployeeId = employeeId,
			SkillId = skill.Id,
			ProficiencyLevel = dto.ProficiencyLevel,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);

		var skills = await _skillRepository.GetEmployeeSkillsAsync(employeeId, cancellationToken);
		return skills.Select(MapSkill).ToList();
	}

	public async Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

		if (employee == null)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		var employeeSkill = await _skillRepository.GetEmployeeSkillAsync(employeeId, skillId, cancellationToken);

		if (employeeSkill == null)
		{
			throw new NotFoundException($"Skill {skillId} is not assigned to employee {employeeId}.");
		}

		await _skillRepository.RemoveEmployeeSkillAsync(employeeSkill, cancellationToken);
	}

	public async Task<TeamDashboardDto> GetTeamDashboardAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var team = await _employeeRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var bench = new List<TeamBenchMemberDto>();
		var active = new List<TeamActiveMemberDto>();

		foreach (var employee in team)
		{
			var utilization = UtilizationCalculator.CalculateCurrentUtilization(employee.ProjectAllocations, today);

			if (utilization == 0)
			{
				bench.Add(new TeamBenchMemberDto
				{
					Id = employee.Id,
					FullName = employee.User.FullName,
					Department = employee.Department,
					Skills = employee.EmployeeSkills.Select(skill => skill.Skill.SkillName).ToList()
				});
			}
			else
			{
				active.Add(new TeamActiveMemberDto
				{
					Id = employee.Id,
					FullName = employee.User.FullName,
					CurrentUtilizationPercent = utilization,
					AvailabilityPercent = 100 - utilization
				});
			}
		}

		return new TeamDashboardDto
		{
			BenchMembers = bench,
			ActiveMembers = active
		};
	}

	public async Task<TeamMemberDetailDto> GetTeamMemberDetailAsync(
		long employeeId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetTeamMemberAsync(employeeId, managerUserId, cancellationToken);

		if (employee == null)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found on your team.");
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var utilization = UtilizationCalculator.CalculateCurrentUtilization(employee.ProjectAllocations, today);
		var fourWeeksAgo = today.AddDays(-28);

		var recentTags = await _context.TimesheetLineItemActivityTags
			.Include(tag => tag.ActivityTag)
			.Include(tag => tag.TimesheetLineItem)
			.ThenInclude(lineItem => lineItem.Timesheet)
			.Where(tag =>
				tag.TimesheetLineItem.Timesheet.EmployeeId == employeeId
				&& tag.TimesheetLineItem.Timesheet.WeekStartDate >= fourWeeksAgo)
			.Select(tag => tag.ActivityTag.TagName)
			.Distinct()
			.ToListAsync(cancellationToken);

		return new TeamMemberDetailDto
		{
			Id = employee.Id,
			FullName = employee.User.FullName,
			Department = employee.Department,
			EmploymentStatus = employee.EmploymentStatus,
			CurrentUtilizationPercent = utilization,
			Skills = employee.EmployeeSkills.Select(skill => skill.Skill.SkillName).ToList(),
			ActiveAllocations = employee.ProjectAllocations
				.Where(allocation => allocation.AllocationStatus == "ACTIVE")
				.Select(allocation => new TeamMemberAllocationDto
				{
					ProjectName = allocation.Project.ProjectName,
					AllocationPercentage = allocation.AllocationPercentage,
					AllocationStartDate = allocation.AllocationStartDate,
					AllocationEndDate = allocation.AllocationEndDate
				})
				.ToList(),
			RecentActivityTags = recentTags
		};
	}

	public async Task AssignManagerAsync(long employeeId, AssignManagerDto dto, CancellationToken cancellationToken = default)
	{
		var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

		if (employee == null || !employee.IsActive)
		{
			throw new NotFoundException($"Employee with ID {employeeId} was not found.");
		}

		var manager = await _userRepository.GetByIdAsync(dto.ManagerUserId, cancellationToken);

		if (manager == null || !manager.IsActive)
		{
			throw new NotFoundException($"Manager user with ID {dto.ManagerUserId} was not found.");
		}

		if (manager.Role != Roles.Manager)
		{
			throw new ValidationException("Specified user is not a manager.");
		}

		if (employee.UserId == dto.ManagerUserId)
		{
			throw new ValidationException("An employee cannot be their own manager.");
		}

		employee.ManagerId = dto.ManagerUserId;
		employee.UpdatedAt = DateTime.UtcNow;
		await _employeeRepository.SaveChangesAsync(cancellationToken);
	}

	private static EmployeeListItemDto MapListItem(Employee employee)
	{
		return new EmployeeListItemDto
		{
			Id = employee.Id,
			EmployeeCode = employee.EmployeeCode,
			FullName = employee.User.FullName,
			Department = employee.Department,
			Designation = employee.Designation,
			EmploymentStatus = employee.EmploymentStatus,
			IsActive = employee.IsActive,
			ManagerId = employee.ManagerId,
			ManagerName = employee.Manager?.FullName,
			Skills = employee.EmployeeSkills
				.Select(employeeSkill => employeeSkill.Skill.SkillName)
				.ToList()
		};
	}

	private static EmployeeSkillDto MapSkill(EmployeeSkill employeeSkill)
	{
		return new EmployeeSkillDto
		{
			SkillId = employeeSkill.SkillId,
			SkillName = employeeSkill.Skill.SkillName,
			Category = employeeSkill.Skill.Category,
			ProficiencyLevel = employeeSkill.ProficiencyLevel
		};
	}
}
