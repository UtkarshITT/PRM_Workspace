namespace PRM.Server.Models.DTOs.Timesheets;

public class TimesheetListItemDto
{
	public long Id { get; set; }
	public DateOnly WeekStartDate { get; set; }
	public decimal TotalHours { get; set; }
	public string Status { get; set; } = string.Empty;
}
