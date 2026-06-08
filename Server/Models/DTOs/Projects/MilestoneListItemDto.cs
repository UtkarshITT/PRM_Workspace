namespace PRM.Server.Models.DTOs.Projects;

public class MilestoneListItemDto
{
	public long Id { get; set; }
	public short SortOrder { get; set; }
	public string MilestoneTitle { get; set; } = string.Empty;
	public DateOnly DueDate { get; set; }
	public int StoryPoints { get; set; }
	public string MilestoneStatus { get; set; } = string.Empty;
}
