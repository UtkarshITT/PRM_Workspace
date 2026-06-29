namespace PRM.Server.Models.Entities;

public class Project
{
	public long Id { get; set; }
	public string ProjectCode { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public string ProjectStatus { get; set; } = "PLANNED";
	public string HealthStatus { get; set; } = "GREEN";
	public string? LastRiskSummary { get; set; }
	public int TotalStoryPoints { get; set; }
	public long ManagerUserId { get; set; }
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public User ManagerUser { get; set; } = null!;
	public ICollection<ProjectMilestone> Milestones { get; set; } = [];
	public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = [];
}
