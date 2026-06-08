namespace PRM.Server.Models.DTOs.Projects;

public class ProjectListItemDto
{
	public long Id { get; set; }
	public string ProjectCode { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public string ManagerName { get; set; } = string.Empty;
	public DateOnly EndDate { get; set; }
	public string ProjectStatus { get; set; } = string.Empty;
	public int StoryPointsDone { get; set; }
	public int TotalStoryPoints { get; set; }
}
