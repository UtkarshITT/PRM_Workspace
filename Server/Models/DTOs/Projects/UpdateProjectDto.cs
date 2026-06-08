namespace PRM.Server.Models.DTOs.Projects;

public class UpdateProjectDto
{
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public string ProjectStatus { get; set; } = string.Empty;
	public long ManagerUserId { get; set; }
	public int TotalStoryPoints { get; set; }
}
