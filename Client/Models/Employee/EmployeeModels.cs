namespace PRM.Client.Models.Employee;

public class ActivityTagItem
{
	public long Id { get; set; }
	public string TagCode { get; set; } = string.Empty;
	public string TagName { get; set; } = string.Empty;
}

public class EmployeeAllocationItem
{
	public long Id { get; set; }
	public long ProjectId { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public string AllocationStartDate { get; set; } = string.Empty;
	public string AllocationEndDate { get; set; } = string.Empty;
	public string AllocationStatus { get; set; } = string.Empty;
}

public class EmployeeAllocationsResponse
{
	public List<EmployeeAllocationItem> Allocations { get; set; } = [];
	public decimal TotalActiveUtilizationPercent { get; set; }
}

public class TimesheetLineItemRequest
{
	public long ProjectId { get; set; }
	public decimal HoursLogged { get; set; }
	public List<long> ActivityTagIds { get; set; } = [];
	public string? CustomTagText { get; set; }
}

public class SubmitTimesheetRequest
{
	public string WeekStartDate { get; set; } = string.Empty;
	public List<TimesheetLineItemRequest> LineItems { get; set; } = [];
	public string? Remarks { get; set; }
}

public class TimesheetSubmittedResponse
{
	public long TimesheetId { get; set; }
	public string WeekStartDate { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public decimal TotalHours { get; set; }
}

public class TimesheetListItem
{
	public long Id { get; set; }
	public string WeekStartDate { get; set; } = string.Empty;
	public decimal TotalHours { get; set; }
	public string Status { get; set; } = string.Empty;
}

public class TimesheetDetail
{
	public long Id { get; set; }
	public string WeekStartDate { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public decimal TotalHours { get; set; }
	public string? Remarks { get; set; }
	public List<TimesheetDetailLineItem> LineItems { get; set; } = [];
}

public class TimesheetDetailLineItem
{
	public string ProjectName { get; set; } = string.Empty;
	public decimal HoursLogged { get; set; }
	public List<string> ActivityTags { get; set; } = [];
}

public class TimesheetRemindersResponse
{
	public List<string> Messages { get; set; } = [];
}
