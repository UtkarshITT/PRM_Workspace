using Microsoft.EntityFrameworkCore;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class ProjectRepository : IProjectRepository
{
	private readonly PrmDbContext _context;

	public ProjectRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<Project?> GetByIdAsync(long projectId, CancellationToken cancellationToken = default)
	{
		return _context.Projects
			.Include(project => project.ManagerUser)
			.Include(project => project.Milestones)
			.FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
	}

	public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Projects
			.Include(project => project.ManagerUser)
			.Include(project => project.Milestones)
			.OrderBy(project => project.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
	{
		_context.Projects.Add(project);
		await _context.SaveChangesAsync(cancellationToken);
		return project;
	}

	public Task<ProjectMilestone?> GetMilestoneAsync(
		long projectId,
		long milestoneId,
		CancellationToken cancellationToken = default)
	{
		return _context.ProjectMilestones
			.FirstOrDefaultAsync(
				milestone => milestone.ProjectId == projectId && milestone.Id == milestoneId,
				cancellationToken);
	}

	public async Task<IReadOnlyList<ProjectMilestone>> GetMilestonesAsync(
		long projectId,
		CancellationToken cancellationToken = default)
	{
		return await _context.ProjectMilestones
			.Where(milestone => milestone.ProjectId == projectId)
			.OrderBy(milestone => milestone.SortOrder)
			.ToListAsync(cancellationToken);
	}

	public async Task<ProjectMilestone> AddMilestoneAsync(
		ProjectMilestone milestone,
		CancellationToken cancellationToken = default)
	{
		_context.ProjectMilestones.Add(milestone);
		await _context.SaveChangesAsync(cancellationToken);
		return milestone;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		return await _context.Projects
			.Where(project => project.ManagerUserId == managerUserId && project.IsActive)
			.OrderBy(project => project.ProjectName)
			.ToListAsync(cancellationToken);
	}

	public Task<Project?> GetDetailByIdAsync(long projectId, CancellationToken cancellationToken = default)
	{
		return _context.Projects
			.Include(project => project.Milestones)
			.Include(project => project.ProjectAllocations)
			.ThenInclude(allocation => allocation.ResourceProfile)
			.ThenInclude(employee => employee.User)
			.FirstOrDefaultAsync(project => project.Id == projectId && project.IsActive, cancellationToken);
	}

	public async Task<IReadOnlyList<Project>> GetActiveProjectsWithMilestonesAsync(
		CancellationToken cancellationToken = default)
	{
		return await _context.Projects
			.Include(project => project.Milestones)
			.Include(project => project.ProjectAllocations)
			.Where(project => project.IsActive && project.ProjectStatus == "ACTIVE")
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<Project>> GetActiveAtRiskProjectsAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Projects
			.Include(project => project.ManagerUser)
			.Where(project =>
				project.IsActive
				&& project.ProjectStatus == ProjectStatuses.Active
				&& (project.HealthStatus == "AMBER" || project.HealthStatus == "RED"))
			.ToListAsync(cancellationToken);
	}

	public async Task UpdateHealthStatusAsync(
		long projectId,
		string healthStatus,
		CancellationToken cancellationToken = default)
	{
		var project = await _context.Projects.FindAsync([projectId], cancellationToken);

		if (project == null)
		{
			return;
		}

		project.HealthStatus = healthStatus;
		project.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task UpdateLastRiskSummaryAsync(
		long projectId,
		string? summary,
		CancellationToken cancellationToken = default)
	{
		var project = await _context.Projects.FindAsync([projectId], cancellationToken);

		if (project == null)
		{
			return;
		}

		project.LastRiskSummary = summary;
		project.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync(cancellationToken);
	}
}
