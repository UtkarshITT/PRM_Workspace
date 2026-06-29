namespace PRM.Server.Models.DTOs.Auth;

public class LoginResponseDto
{
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }
	public long UserId { get; set; }
	public long? ResourceProfileId { get; set; }
	public string Role { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public bool ForcePasswordChange { get; set; }
}
