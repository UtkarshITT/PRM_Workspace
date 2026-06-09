namespace PRM.Server.Models.DTOs.Timesheets;

public class SubmitTimesheetDto
{
	public DateOnly WeekStartDate { get; set; }
	public List<TimesheetLineItemDto> LineItems { get; set; } = [];
	public string? Remarks { get; set; }
}

public class TimesheetLineItemDto
{
	public long ProjectId { get; set; }
	public decimal HoursLogged { get; set; }
	public List<long> ActivityTagIds { get; set; } = [];
	public string? CustomTagText { get; set; }
}
