namespace PRM.Client.Models.Admin;

public class CreateUserRequest
{
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string TemporaryPassword { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
}

public class UserCreatedResponse
{
	public long UserId { get; set; }
	public long EmployeeId { get; set; }
	public string EmployeeCode { get; set; } = string.Empty;
}

public class UserListItem
{
	public long Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}

public class ResetPasswordRequest
{
	public string NewTemporaryPassword { get; set; } = string.Empty;
}

public class EmployeeListItem
{
	public long Id { get; set; }
	public string EmployeeCode { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
	public string EmploymentStatus { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public long? ManagerId { get; set; }
	public string? ManagerName { get; set; }
	public List<string> Skills { get; set; } = [];
}

public class UpdateEmployeeRequest
{
	public string FullName { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
}

public class AddSkillRequest
{
	public string SkillName { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string ProficiencyLevel { get; set; } = string.Empty;
}

public class EmployeeSkillItem
{
	public long SkillId { get; set; }
	public string SkillName { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string ProficiencyLevel { get; set; } = string.Empty;
}

public class AssignManagerRequest
{
	public long ManagerUserId { get; set; }
}
