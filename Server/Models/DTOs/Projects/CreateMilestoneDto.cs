namespace PRM.Server.Models.DTOs.Projects;

public class CreateMilestoneDto
{
	public string MilestoneTitle { get; set; } = string.Empty;
	public DateOnly DueDate { get; set; }
	public int StoryPoints { get; set; }
	public short SortOrder { get; set; }
}
