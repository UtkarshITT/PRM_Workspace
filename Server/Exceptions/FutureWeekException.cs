namespace PRM.Server.Exceptions;

public class FutureWeekException : Exception
{
	public FutureWeekException()
		: base("Cannot submit a timesheet for a future week.")
	{
	}
}
