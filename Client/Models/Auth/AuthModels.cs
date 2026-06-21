namespace PRM.Client.Models.Auth;

public class LoginRequest
{
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

public class PasswordChangeRequest
{
	public string CurrentPassword { get; set; } = string.Empty;
	public string NewPassword { get; set; } = string.Empty;
}

public class LoginResponse
{
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }
	public long UserId { get; set; }
	public long? ResourceProfileId { get; set; }
	public string Role { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public bool ForcePasswordChange { get; set; }
}
