namespace PRM.Server.Exceptions;

public class OverAllocationException : Exception
{
	public OverAllocationException(string employeeName, decimal totalUtilization)
		: base($"{employeeName} would be at {totalUtilization:0}% utilisation. Maximum is 100%.")
	{
	}
}
