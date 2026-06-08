namespace PRM.Server.Models.DTOs.Projects;

public class ProjectCreatedDto
{
	public long ProjectId { get; set; }
	public string ProjectCode { get; set; } = string.Empty;
}
