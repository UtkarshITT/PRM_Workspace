namespace PRM.Server.Models.DTOs.Users;

public class RolePermissionDto
{
	public string Role { get; set; } = string.Empty;
	public IReadOnlyList<string> Permissions { get; set; } = [];
}
