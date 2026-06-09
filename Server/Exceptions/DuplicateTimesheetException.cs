namespace PRM.Server.Exceptions;

public class DuplicateTimesheetException : Exception
{
	public DuplicateTimesheetException(DateOnly weekStart)
		: base($"A timesheet for week {weekStart:dd-MM-yyyy} has already been submitted.")
	{
	}
}
