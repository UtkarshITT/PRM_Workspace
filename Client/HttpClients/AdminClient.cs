using PRM.Client.Models;
using PRM.Client.Models.Admin;

namespace PRM.Client.HttpClients;

public class AdminClient
{
	private readonly RestClient _restClient;

	public AdminClient(RestClient restClient)
	{
		_restClient = restClient;
	}

	public Task<ApiResponse<UserCreatedResponse>> CreateUserAsync(CreateUserRequest request) =>
		_restClient.PostAsync<UserCreatedResponse>("/api/users", request, requireAuth: true);

	public Task<ApiResponse<List<UserListItem>>> GetUsersAsync() =>
		_restClient.GetAsync<List<UserListItem>>("/api/users", requireAuth: true);

	public Task<ApiResponse<object>> ResetPasswordAsync(long userId, ResetPasswordRequest request) =>
		_restClient.PutAsync<object>($"/api/users/{userId}/reset-password", request, requireAuth: true);

	public Task<ApiResponse<object>> UpdateUserRoleAsync(long userId, UpdateUserRoleRequest request) =>
		_restClient.PutAsync<object>($"/api/users/{userId}/role", request, requireAuth: true);

	public Task<ApiResponse<List<RolePermissionItem>>> GetRolePermissionsAsync() =>
		_restClient.GetAsync<List<RolePermissionItem>>("/api/users/role-permissions", requireAuth: true);

	public Task<ApiResponse<object>> DeactivateUserAsync(long userId) =>
		_restClient.PutAsync<object>($"/api/users/{userId}/deactivate", new { }, requireAuth: true);

	public Task<ApiResponse<object>> ReactivateUserAsync(long userId) =>
		_restClient.PutAsync<object>($"/api/users/{userId}/reactivate", new { }, requireAuth: true);

	public Task<ApiResponse<List<EmployeeListItem>>> GetEmployeesAsync(string? status = null, string? department = null)
	{
		var query = new List<string>();
		if (!string.IsNullOrWhiteSpace(status))
		{
			query.Add($"status={Uri.EscapeDataString(status)}");
		}

		if (!string.IsNullOrWhiteSpace(department))
		{
			query.Add($"department={Uri.EscapeDataString(department)}");
		}

		var path = query.Count == 0
			? "/api/employees"
			: $"/api/employees?{string.Join("&", query)}";

		return _restClient.GetAsync<List<EmployeeListItem>>(path, requireAuth: true);
	}

	public Task<ApiResponse<object>> UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequest request) =>
		_restClient.PutAsync<object>($"/api/employees/{employeeId}", request, requireAuth: true);

	public Task<ApiResponse<object>> DeactivateEmployeeAsync(long employeeId) =>
		_restClient.PutAsync<object>($"/api/employees/{employeeId}/deactivate", new { }, requireAuth: true);

	public Task<ApiResponse<List<EmployeeSkillItem>>> AddSkillAsync(long employeeId, AddSkillRequest request) =>
		_restClient.PostAsync<List<EmployeeSkillItem>>($"/api/employees/{employeeId}/skills", request, requireAuth: true);

	public Task<ApiResponse<object>> RemoveSkillAsync(long employeeId, long skillId) =>
		_restClient.DeleteAsync<object>($"/api/employees/{employeeId}/skills/{skillId}", requireAuth: true);

	public Task<ApiResponse<object>> AssignManagerAsync(long employeeId, AssignManagerRequest request) =>
		_restClient.PutAsync<object>($"/api/employees/{employeeId}/manager", request, requireAuth: true);

	public Task<ApiResponse<ProjectCreatedResponse>> CreateProjectAsync(CreateProjectRequest request) =>
		_restClient.PostAsync<ProjectCreatedResponse>("/api/projects", request, requireAuth: true);

	public Task<ApiResponse<List<ProjectListItem>>> GetProjectsAsync() =>
		_restClient.GetAsync<List<ProjectListItem>>("/api/projects", requireAuth: true);

	public Task<ApiResponse<object>> UpdateProjectAsync(long projectId, UpdateProjectRequest request) =>
		_restClient.PutAsync<object>($"/api/projects/{projectId}", request, requireAuth: true);

	public Task<ApiResponse<List<MilestoneListItem>>> GetMilestonesAsync(long projectId) =>
		_restClient.GetAsync<List<MilestoneListItem>>($"/api/projects/{projectId}/milestones", requireAuth: true);

	public Task<ApiResponse<MilestoneListItem>> AddMilestoneAsync(long projectId, CreateMilestoneRequest request) =>
		_restClient.PostAsync<MilestoneListItem>($"/api/projects/{projectId}/milestones", request, requireAuth: true);

	public Task<ApiResponse<object>> UpdateMilestoneStatusAsync(
		long projectId,
		long milestoneId,
		UpdateMilestoneStatusRequest request) =>
		_restClient.PutAsync<object>($"/api/projects/{projectId}/milestones/{milestoneId}", request, requireAuth: true);

	public Task<ApiResponse<List<AllocationListItem>>> GetAllocationsAsync(long? employeeId = null, long? projectId = null)
	{
		var query = new List<string>();
		if (employeeId.HasValue)
		{
			query.Add($"employeeId={employeeId.Value}");
		}

		if (projectId.HasValue)
		{
			query.Add($"projectId={projectId.Value}");
		}

		var path = query.Count == 0
			? "/api/allocations"
			: $"/api/allocations?{string.Join("&", query)}";

		return _restClient.GetAsync<List<AllocationListItem>>(path, requireAuth: true);
	}

	public Task<ApiResponse<List<SystemConfigItem>>> GetSystemConfigAsync() =>
		_restClient.GetAsync<List<SystemConfigItem>>("/api/system-config", requireAuth: true);

	public Task<ApiResponse<List<SystemConfigItem>>> UpdateSystemConfigAsync(UpdateSystemConfigRequest request) =>
		_restClient.PutAsync<List<SystemConfigItem>>("/api/system-config", request, requireAuth: true);

	public Task<ApiResponse<List<NotificationLogItem>>> GetNotificationLogsAsync(int take = 50) =>
		_restClient.GetAsync<List<NotificationLogItem>>($"/api/notifications/logs?take={take}", requireAuth: true);
}
