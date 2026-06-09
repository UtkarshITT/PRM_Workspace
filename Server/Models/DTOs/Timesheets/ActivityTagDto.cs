namespace PRM.Server.Models.DTOs.Timesheets;

public class ActivityTagDto
{
	public long Id { get; set; }
	public string TagCode { get; set; } = string.Empty;
	public string TagName { get; set; } = string.Empty;
}
