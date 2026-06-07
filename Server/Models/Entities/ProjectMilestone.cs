namespace PRM.Server.Models.Entities;

public class ProjectMilestone
{
	public long Id { get; set; }
	public long ProjectId { get; set; }
	public string MilestoneTitle { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateOnly DueDate { get; set; }
	public string MilestoneStatus { get; set; } = "NOT_STARTED";
	public int StoryPoints { get; set; }
	public short SortOrder { get; set; }
	public DateTime? CompletedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public Project Project { get; set; } = null!;
}
