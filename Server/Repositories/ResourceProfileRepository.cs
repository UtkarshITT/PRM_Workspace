using Microsoft.EntityFrameworkCore;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Helpers;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class ResourceProfileRepository : IResourceProfileRepository
{
	private readonly PrmDbContext _context;

	public ResourceProfileRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<ResourceProfile?> GetByIdAsync(long resourceProfileId, CancellationToken cancellationToken = default)
	{
		return _context.ResourceProfiles
			.Include(resourceProfile => resourceProfile.User)
			.Include(resourceProfile => resourceProfile.Manager)
			.Include(resourceProfile => resourceProfile.ResourceProfileSkills)
			.ThenInclude(resourceProfileSkill => resourceProfileSkill.Skill)
			.FirstOrDefaultAsync(resourceProfile => resourceProfile.Id == resourceProfileId, cancellationToken);
	}

	public Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return _context.ResourceProfiles
			.FirstOrDefaultAsync(resourceProfile => resourceProfile.UserId == userId, cancellationToken);
	}

	public async Task<IReadOnlyList<ResourceProfile>> GetByManagerIdAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ResourceProfiles
			.Include(resourceProfile => resourceProfile.User)
			.Include(resourceProfile => resourceProfile.ResourceProfileSkills)
			.ThenInclude(resourceProfileSkill => resourceProfileSkill.Skill)
			.Include(resourceProfile => resourceProfile.ProjectAllocations)
			.Where(resourceProfile => resourceProfile.ManagerId == managerUserId && resourceProfile.IsActive)
			.OrderBy(resourceProfile => resourceProfile.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ResourceProfile>> GetActiveOrganizationCandidatesAsync(
		CancellationToken cancellationToken = default)
	{
		return await _context.ResourceProfiles
			.Include(resourceProfile => resourceProfile.User)
			.Include(resourceProfile => resourceProfile.Manager)
			.Include(resourceProfile => resourceProfile.ResourceProfileSkills)
			.ThenInclude(resourceProfileSkill => resourceProfileSkill.Skill)
			.Include(resourceProfile => resourceProfile.ProjectAllocations)
			.Where(resourceProfile => resourceProfile.User.Role == Roles.Employee && resourceProfile.IsActive)
			.OrderBy(resourceProfile => resourceProfile.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<ResourceProfile>> GetAllocatedForWeekAsync(
		DateOnly weekStart,
		DateOnly weekEnd,
		CancellationToken cancellationToken = default)
	{
		var allocations = await _context.ProjectAllocations
			.Include(allocation => allocation.ResourceProfile)
			.ThenInclude(profile => profile.User)
			.Include(allocation => allocation.ResourceProfile)
			.ThenInclude(profile => profile.Manager)
			.Where(allocation => allocation.AllocationStatus == "ACTIVE")
			.ToListAsync(cancellationToken);

		return allocations
			.Where(allocation => UtilizationCalculator.PeriodsOverlap(
				allocation.AllocationStartDate,
				allocation.AllocationEndDate,
				weekStart,
				weekEnd))
			.Select(allocation => allocation.ResourceProfile)
			.Where(profile => profile.IsActive)
			.GroupBy(profile => profile.Id)
			.Select(group => group.First())
			.ToList();
	}

	public Task<ResourceProfile?> GetTeamMemberAsync(
		long resourceProfileId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		return _context.ResourceProfiles
			.Include(resourceProfile => resourceProfile.User)
			.Include(resourceProfile => resourceProfile.ResourceProfileSkills)
			.ThenInclude(resourceProfileSkill => resourceProfileSkill.Skill)
			.Include(resourceProfile => resourceProfile.ProjectAllocations)
			.ThenInclude(allocation => allocation.Project)
			.FirstOrDefaultAsync(
				resourceProfile => resourceProfile.Id == resourceProfileId && resourceProfile.ManagerId == managerUserId && resourceProfile.IsActive,
				cancellationToken);
	}

	public async Task<IReadOnlyList<ResourceProfile>> GetAllAsync(
		string? status,
		string? department,
		CancellationToken cancellationToken = default)
	{
		var query = _context.ResourceProfiles
			.Include(resourceProfile => resourceProfile.User)
			.Include(resourceProfile => resourceProfile.Manager)
			.Include(resourceProfile => resourceProfile.ResourceProfileSkills)
			.ThenInclude(resourceProfileSkill => resourceProfileSkill.Skill)
			.Where(resourceProfile => resourceProfile.User.Role == Roles.Employee)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(status))
		{
			query = query.Where(resourceProfile => resourceProfile.EmploymentStatus == status);
		}

		if (!string.IsNullOrWhiteSpace(department))
		{
			query = query.Where(resourceProfile => resourceProfile.Department == department);
		}

		return await query
			.OrderBy(resourceProfile => resourceProfile.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<ResourceProfile> AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default)
	{
		_context.ResourceProfiles.Add(resourceProfile);
		await _context.SaveChangesAsync(cancellationToken);
		return resourceProfile;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}
}
