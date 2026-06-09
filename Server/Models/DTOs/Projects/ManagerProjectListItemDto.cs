namespace PRM.Server.Models.DTOs.Projects;

public class ManagerProjectListItemDto
{
	public long Id { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public DateOnly EndDate { get; set; }
	public string HealthStatus { get; set; } = string.Empty;
	public string ProjectStatus { get; set; } = string.Empty;
}
