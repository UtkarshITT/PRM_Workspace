using PRM.Client.Models;
using PRM.Client.Models.Manager;

namespace PRM.Client.HttpClients;

public class ManagerClient
{
	private readonly RestClient _restClient;

	public ManagerClient(RestClient restClient)
	{
		_restClient = restClient;
	}

	public Task<ApiResponse<TeamDashboardResponse>> GetMyTeamAsync() =>
		_restClient.GetAsync<TeamDashboardResponse>("/api/employees/my-team", requireAuth: true);

	public Task<ApiResponse<TeamMemberDetail>> GetTeamMemberAsync(long employeeId) =>
		_restClient.GetAsync<TeamMemberDetail>($"/api/employees/{employeeId}", requireAuth: true);

	public Task<ApiResponse<AllocationCreatedResponse>> CreateAllocationAsync(CreateAllocationRequest request) =>
		_restClient.PostAsync<AllocationCreatedResponse>("/api/allocations", request, requireAuth: true);

	public Task<ApiResponse<object>> EndAllocationAsync(long allocationId) =>
		_restClient.PutAsync<object>($"/api/allocations/{allocationId}/end", new { }, requireAuth: true);
}
