namespace PRM.Server.Models.Entities;

public class TimesheetLineItemActivityTag
{
	public long TimesheetLineItemId { get; set; }
	public long ActivityTagId { get; set; }
	public string? CustomTagText { get; set; }

	public TimesheetLineItem TimesheetLineItem { get; set; } = null!;
	public ActivityTag ActivityTag { get; set; } = null!;
}
