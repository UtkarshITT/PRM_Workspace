namespace PRM.Server.Exceptions;

public class ValidationException : Exception
{
	public ValidationException(string message) : base(message)
	{
	}

	public ValidationException(string message, IEnumerable<string> details)
		: base(message)
	{
		Details = details.ToList();
	}

	public List<string> Details { get; } = [];
}
