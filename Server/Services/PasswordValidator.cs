namespace PRM.Server.Services;

public static class PasswordValidator
{
	public static IReadOnlyList<string> Validate(string password)
	{
		var errors = new List<string>();

		if (password.Length < 8)
		{
			errors.Add("Password must be at least 8 characters.");
		}

		if (!password.Any(char.IsUpper))
		{
			errors.Add("Password must contain at least one uppercase letter.");
		}

		if (!password.Any(char.IsDigit))
		{
			errors.Add("Password must contain at least one number.");
		}

		return errors;
	}
}
