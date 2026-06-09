namespace PRM.Server.Models.DTOs.Timesheets;

public class TimesheetDetailDto
{
	public long Id { get; set; }
	public DateOnly WeekStartDate { get; set; }
	public string Status { get; set; } = string.Empty;
	public decimal TotalHours { get; set; }
	public string? Remarks { get; set; }
	public IReadOnlyList<TimesheetDetailLineItemDto> LineItems { get; set; } = [];
}

public class TimesheetDetailLineItemDto
{
	public string ProjectName { get; set; } = string.Empty;
	public decimal HoursLogged { get; set; }
	public IReadOnlyList<string> ActivityTags { get; set; } = [];
}
