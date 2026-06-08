using PRM.Server.Constants;

namespace PRM.Server.Models.DTOs.Projects;

public class CreateProjectDto
{
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public string ProjectStatus { get; set; } = ProjectStatuses.Planned;
	public long ManagerUserId { get; set; }
	public int TotalStoryPoints { get; set; }
}
