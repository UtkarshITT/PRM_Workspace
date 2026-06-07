namespace PRM.Client.Helpers;

public static class SessionStore
{
	public static string? Token { get; private set; }
	public static string? Role { get; private set; }
	public static string? FullName { get; private set; }
	public static long? UserId { get; private set; }
	public static long? EmployeeId { get; private set; }
	public static bool ForcePasswordChange { get; private set; }

	public static void SetSession(Models.Auth.LoginResponse response)
	{
		Token = response.Token;
		Role = response.Role;
		FullName = response.FullName;
		UserId = response.UserId;
		EmployeeId = response.EmployeeId;
		ForcePasswordChange = response.ForcePasswordChange;
	}

	public static void Clear()
	{
		Token = null;
		Role = null;
		FullName = null;
		UserId = null;
		EmployeeId = null;
		ForcePasswordChange = false;
	}

	public static void MarkPasswordChanged()
	{
		ForcePasswordChange = false;
	}

	public static bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
}
