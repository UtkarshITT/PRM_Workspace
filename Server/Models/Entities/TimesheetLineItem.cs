namespace PRM.Server.Models.Entities;

public class TimesheetLineItem
{
	public long Id { get; set; }
	public long TimesheetId { get; set; }
	public long ProjectId { get; set; }
	public decimal HoursLogged { get; set; }
	public string? WorkNotes { get; set; }
	public DateOnly? WorkDate { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public Timesheet Timesheet { get; set; } = null!;
	public Project Project { get; set; } = null!;
	public ICollection<TimesheetLineItemActivityTag> ActivityTags { get; set; } = [];
}
