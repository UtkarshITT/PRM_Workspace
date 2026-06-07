namespace PRM.Server.Models.Entities;

public class Timesheet
{
	public long Id { get; set; }
	public long EmployeeId { get; set; }
	public DateOnly WeekStartDate { get; set; }
	public string Status { get; set; } = "SUBMITTED";
	public decimal TotalHours { get; set; }
	public string? Remarks { get; set; }
	public DateTime? SubmittedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public Employee Employee { get; set; } = null!;
	public ICollection<TimesheetLineItem> LineItems { get; set; } = [];
}
