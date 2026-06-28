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

public class UpdateUserRoleRequest
{
	public string Role { get; set; } = string.Empty;
}

public class RolePermissionItem
{
	public string Role { get; set; } = string.Empty;
	public List<string> Permissions { get; set; } = [];
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
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
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

public class CreateProjectRequest
{
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string StartDate { get; set; } = string.Empty;
	public string EndDate { get; set; } = string.Empty;
	public string ProjectStatus { get; set; } = string.Empty;
	public long ManagerUserId { get; set; }
	public int TotalStoryPoints { get; set; }
}

public class UpdateProjectRequest
{
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string StartDate { get; set; } = string.Empty;
	public string EndDate { get; set; } = string.Empty;
	public string ProjectStatus { get; set; } = string.Empty;
	public long ManagerUserId { get; set; }
	public int TotalStoryPoints { get; set; }
}

public class ProjectListItem
{
	public long Id { get; set; }
	public string ProjectCode { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string ManagerName { get; set; } = string.Empty;
	public long ManagerUserId { get; set; }
	public string StartDate { get; set; } = string.Empty;
	public string EndDate { get; set; } = string.Empty;
	public string ProjectStatus { get; set; } = string.Empty;
	public int StoryPointsDone { get; set; }
	public int TotalStoryPoints { get; set; }
}

public class ProjectCreatedResponse
{
	public long ProjectId { get; set; }
	public string ProjectCode { get; set; } = string.Empty;
}

public class CreateMilestoneRequest
{
	public string MilestoneTitle { get; set; } = string.Empty;
	public string DueDate { get; set; } = string.Empty;
	public int StoryPoints { get; set; }
	public short SortOrder { get; set; }
}

public class UpdateMilestoneStatusRequest
{
	public string MilestoneStatus { get; set; } = string.Empty;
}

public class MilestoneListItem
{
	public long Id { get; set; }
	public short SortOrder { get; set; }
	public string MilestoneTitle { get; set; } = string.Empty;
	public string DueDate { get; set; } = string.Empty;
	public int StoryPoints { get; set; }
	public string MilestoneStatus { get; set; } = string.Empty;
}

public class AllocationListItem
{
	public long Id { get; set; }
	public string EmployeeName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public decimal AllocationPercentage { get; set; }
	public string AllocationStartDate { get; set; } = string.Empty;
	public string AllocationEndDate { get; set; } = string.Empty;
	public string AllocationStatus { get; set; } = string.Empty;
}

public class SystemConfigItem
{
	public string Key { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
	public string? Description { get; set; }
	public bool IsSecret { get; set; }
	public bool IsConfigured { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class UpdateSystemConfigRequest
{
	public string? LlmProvider { get; set; }
	public string? LlmApiKey { get; set; }
	public int? SchedulerIntervalHours { get; set; }
	public int? MaxWeeklyHours { get; set; }
	public bool? EmailConsoleEnabled { get; set; }
	public bool? EmailSmtpEnabled { get; set; }
}

public class NotificationLogItem
{
	public long Id { get; set; }
	public string NotificationType { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string RecipientEmail { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string DeliveryChannel { get; set; } = string.Empty;
	public string? RelatedEntityName { get; set; }
	public long? RelatedEntityId { get; set; }
	public string? WeekStartDate { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime CreatedAt { get; set; }
}
