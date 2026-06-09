namespace PRM.Server.Models.DTOs.Timesheets;

public class TeamTimesheetRowDto
{
	public long TimesheetId { get; set; }
	public string EmployeeName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public decimal HoursLogged { get; set; }
	public string Status { get; set; } = string.Empty;
}
