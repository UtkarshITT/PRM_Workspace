namespace PRM.Server.Models.DTOs.Users;

public class CreateUserDto
{
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string TemporaryPassword { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
}
