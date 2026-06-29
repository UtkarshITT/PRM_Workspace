using PRM.Server.Constants;
using PRM.Server.Helpers;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Employees;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IResourceProfileService
{
	Task<IReadOnlyList<EmployeeListItemDto>> GetAllEmployeesAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default);
	Task UpdateEmployeeAsync(long resourceProfileId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
	Task DeactivateEmployeeAsync(long resourceProfileId, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<EmployeeSkillDto>> AddSkillAsync(long resourceProfileId, AddSkillDto dto, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<EmployeeSkillDto>> UpdateSkillProficiencyAsync(
		long resourceProfileId,
		long skillId,
		UpdateSkillProficiencyDto dto,
		CancellationToken cancellationToken = default);
	Task RemoveSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default);
	Task AssignManagerAsync(long resourceProfileId, AssignManagerDto dto, CancellationToken cancellationToken = default);
	Task<TeamDashboardDto> GetTeamDashboardAsync(long managerUserId, CancellationToken cancellationToken = default);
	Task<TeamMemberDetailDto> GetTeamMemberDetailAsync(long resourceProfileId, long managerUserId, CancellationToken cancellationToken = default);
	Task RestoreTimesheetAccessAsync(long resourceProfileId, long managerUserId, CancellationToken cancellationToken = default);
}

public class ResourceProfileService : IResourceProfileService
{
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IUserRepository _userRepository;
	private readonly ISkillRepository _skillRepository;
	private readonly IAllocationRepository _allocationRepository;
	private readonly ITimesheetRepository _timesheetRepository;
	private readonly IAuditLogRepository _auditLogRepository;

	public ResourceProfileService(
		IResourceProfileRepository resourceProfileRepository,
		IUserRepository userRepository,
		ISkillRepository skillRepository,
		IAllocationRepository allocationRepository,
		ITimesheetRepository timesheetRepository,
		IAuditLogRepository auditLogRepository)
	{
		_resourceProfileRepository = resourceProfileRepository;
		_userRepository = userRepository;
		_skillRepository = skillRepository;
		_allocationRepository = allocationRepository;
		_timesheetRepository = timesheetRepository;
		_auditLogRepository = auditLogRepository;
	}

	public async Task<IReadOnlyList<EmployeeListItemDto>> GetAllEmployeesAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default)
	{
		var resourceProfiles = await _resourceProfileRepository.GetAllAsync(status, department, cancellationToken);

		return resourceProfiles.Select(MapListItem).ToList();
	}

	public async Task UpdateEmployeeAsync(long resourceProfileId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null || !resourceProfile.IsActive)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
		}

		resourceProfile.User.FullName = dto.FullName;
		resourceProfile.Department = dto.Department;
		resourceProfile.Designation = dto.Designation;
		resourceProfile.UpdatedAt = DateTime.UtcNow;
		resourceProfile.User.UpdatedAt = DateTime.UtcNow;

		await _resourceProfileRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task DeactivateEmployeeAsync(long resourceProfileId, long actorUserId, CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
		}

		if (!resourceProfile.IsActive)
		{
			throw new ValidationException("Employee is already inactive.");
		}

		var activeAllocations = await _allocationRepository.GetActiveByResourceProfileIdAsync(resourceProfileId, cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var now = DateTime.UtcNow;
		var endedAllocationIds = new List<long>();

		resourceProfile.IsActive = false;
		resourceProfile.EmploymentStatus = "BENCH";
		resourceProfile.UpdatedAt = now;
		resourceProfile.User.IsActive = false;
		resourceProfile.User.UpdatedAt = now;

		foreach (var allocation in activeAllocations)
		{
			allocation.AllocationStatus = "ENDED";
			allocation.AllocationEndDate = today;
			allocation.UpdatedAt = now;
			endedAllocationIds.Add(allocation.Id);
		}

		await _resourceProfileRepository.SaveChangesAsync(cancellationToken);
		await _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = actorUserId,
			EntityName = "RESOURCE_PROFILES",
			EntityId = resourceProfileId,
			ActionType = "DEACTIVATE",
			NewValues = $"{{\"endedAllocationIds\":[{string.Join(",", endedAllocationIds)}]}}",
			CreatedAt = now
		}, cancellationToken);
	}

	public async Task<IReadOnlyList<EmployeeSkillDto>> AddSkillAsync(
		long resourceProfileId,
		AddSkillDto dto,
		CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null || !resourceProfile.IsActive)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
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

		var existing = await _skillRepository.GetResourceProfileSkillAsync(resourceProfileId, skill.Id, cancellationToken);

		if (existing != null)
		{
			throw new ConflictException($"Employee already has skill '{dto.SkillName}'.");
		}

		await _skillRepository.AddResourceProfileSkillAsync(new ResourceProfileSkill
		{
			ResourceProfileId = resourceProfileId,
			SkillId = skill.Id,
			ProficiencyLevel = dto.ProficiencyLevel,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);

		var skills = await _skillRepository.GetResourceProfileSkillsAsync(resourceProfileId, cancellationToken);
		return skills.Select(MapSkill).ToList();
	}

	public async Task<IReadOnlyList<EmployeeSkillDto>> UpdateSkillProficiencyAsync(
		long resourceProfileId,
		long skillId,
		UpdateSkillProficiencyDto dto,
		CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null || !resourceProfile.IsActive)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
		}

		var resourceProfileSkill = await _skillRepository.GetResourceProfileSkillAsync(
			resourceProfileId,
			skillId,
			cancellationToken);

		if (resourceProfileSkill == null)
		{
			throw new NotFoundException($"Skill {skillId} is not assigned to employee {resourceProfileId}.");
		}

		resourceProfileSkill.ProficiencyLevel = dto.ProficiencyLevel;
		await _skillRepository.SaveChangesAsync(cancellationToken);

		var skills = await _skillRepository.GetResourceProfileSkillsAsync(resourceProfileId, cancellationToken);
		return skills.Select(MapSkill).ToList();
	}

	public async Task RemoveSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
		}

		var resourceProfileSkill = await _skillRepository.GetResourceProfileSkillAsync(resourceProfileId, skillId, cancellationToken);

		if (resourceProfileSkill == null)
		{
			throw new NotFoundException($"Skill {skillId} is not assigned to employee {resourceProfileId}.");
		}

		await _skillRepository.RemoveResourceProfileSkillAsync(resourceProfileSkill, cancellationToken);
	}

	public async Task<TeamDashboardDto> GetTeamDashboardAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var team = await _resourceProfileRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var bench = new List<TeamBenchMemberDto>();
		var active = new List<TeamActiveMemberDto>();

		foreach (var resourceProfile in team)
		{
			var utilization = UtilizationCalculator.CalculateCurrentUtilization(resourceProfile.ProjectAllocations, today);

			if (utilization == 0)
			{
				bench.Add(new TeamBenchMemberDto
				{
					Id = resourceProfile.Id,
					FullName = resourceProfile.User.FullName,
					Department = resourceProfile.Department,
					Skills = resourceProfile.ResourceProfileSkills.Select(skill => skill.Skill.SkillName).ToList()
				});
			}
			else
			{
				active.Add(new TeamActiveMemberDto
				{
					Id = resourceProfile.Id,
					FullName = resourceProfile.User.FullName,
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
		long resourceProfileId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetTeamMemberAsync(resourceProfileId, managerUserId, cancellationToken);

		if (resourceProfile == null)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found on your team.");
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var utilization = UtilizationCalculator.CalculateCurrentUtilization(resourceProfile.ProjectAllocations, today);
		var fourWeeksAgo = today.AddDays(-28);

		var recentTagsByProfile = await _timesheetRepository.GetRecentActivityTagsByResourceProfilesAsync(
			[resourceProfileId],
			fourWeeksAgo,
			cancellationToken);
		var recentTags = recentTagsByProfile.GetValueOrDefault(resourceProfileId) ?? [];

		return new TeamMemberDetailDto
		{
			Id = resourceProfile.Id,
			FullName = resourceProfile.User.FullName,
			Department = resourceProfile.Department,
			EmploymentStatus = resourceProfile.EmploymentStatus,
			IsTimesheetFrozen = resourceProfile.IsTimesheetFrozen,
			TimesheetFrozenAt = resourceProfile.TimesheetFrozenAt,
			CurrentUtilizationPercent = utilization,
			Skills = resourceProfile.ResourceProfileSkills.Select(skill => skill.Skill.SkillName).ToList(),
			ActiveAllocations = resourceProfile.ProjectAllocations
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

	public async Task AssignManagerAsync(long resourceProfileId, AssignManagerDto dto, CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetByIdAsync(resourceProfileId, cancellationToken);

		if (resourceProfile == null || !resourceProfile.IsActive)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found.");
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

		if (resourceProfile.UserId == dto.ManagerUserId)
		{
			throw new ValidationException("An employee cannot be their own manager.");
		}

		resourceProfile.ManagerId = dto.ManagerUserId;
		resourceProfile.UpdatedAt = DateTime.UtcNow;
		await _resourceProfileRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task RestoreTimesheetAccessAsync(
		long resourceProfileId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		var resourceProfile = await _resourceProfileRepository.GetTeamMemberAsync(
			resourceProfileId,
			managerUserId,
			cancellationToken);

		if (resourceProfile == null)
		{
			throw new NotFoundException($"Employee with ID {resourceProfileId} was not found on your team.");
		}

		if (!resourceProfile.IsTimesheetFrozen)
		{
			throw new ValidationException("Timesheet access is not frozen for this employee.");
		}

		var now = DateTime.UtcNow;
		resourceProfile.IsTimesheetFrozen = false;
		resourceProfile.TimesheetFrozenAt = null;
		resourceProfile.UpdatedAt = now;

		await _timesheetRepository.ClearComplianceFreezeAsync(resourceProfileId, cancellationToken);
		await _resourceProfileRepository.SaveChangesAsync(cancellationToken);
		await _timesheetRepository.SaveChangesAsync(cancellationToken);
		await _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = managerUserId,
			EntityName = "RESOURCE_PROFILES",
			EntityId = resourceProfileId,
			ActionType = "RESTORE_TIMESHEET_ACCESS",
			NewValues = "{\"is_timesheet_frozen\":false}",
			CreatedAt = now
		}, cancellationToken);
	}

	private static EmployeeListItemDto MapListItem(ResourceProfile resourceProfile)
	{
		return new EmployeeListItemDto
		{
			Id = resourceProfile.Id,
			EmployeeCode = resourceProfile.ResourceProfileCode,
			FullName = resourceProfile.User.FullName,
			Department = resourceProfile.Department,
			Designation = resourceProfile.Designation,
			EmploymentStatus = resourceProfile.EmploymentStatus,
			IsActive = resourceProfile.IsActive,
			IsTimesheetFrozen = resourceProfile.IsTimesheetFrozen,
			TimesheetFrozenAt = resourceProfile.TimesheetFrozenAt,
			ManagerId = resourceProfile.ManagerId,
			ManagerName = resourceProfile.Manager?.FullName,
			Skills = resourceProfile.ResourceProfileSkills
				.Select(resourceProfileSkill => resourceProfileSkill.Skill.SkillName)
				.ToList()
		};
	}

	private static EmployeeSkillDto MapSkill(ResourceProfileSkill resourceProfileSkill)
	{
		return new EmployeeSkillDto
		{
			SkillId = resourceProfileSkill.SkillId,
			SkillName = resourceProfileSkill.Skill.SkillName,
			Category = resourceProfileSkill.Skill.Category,
			ProficiencyLevel = resourceProfileSkill.ProficiencyLevel
		};
	}
}
