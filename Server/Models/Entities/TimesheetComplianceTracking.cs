namespace PRM.Server.Models.Entities;

public class TimesheetComplianceTracking
{
	public long ResourceProfileId { get; set; }
	public DateOnly WeekStartDate { get; set; }
	public short ReminderCount { get; set; }
	public DateTime? LastReminderAt { get; set; }
	public bool IsFrozenForWeek { get; set; }

	public ResourceProfile ResourceProfile { get; set; } = null!;
}
