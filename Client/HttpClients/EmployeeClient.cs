using PRM.Client.Models;
using PRM.Client.Models.Employee;

namespace PRM.Client.HttpClients;

public class EmployeeClient
{
	private readonly RestClient _restClient;

	public EmployeeClient(RestClient restClient)
	{
		_restClient = restClient;
	}

	public Task<ApiResponse<TimesheetRemindersResponse>> GetRemindersAsync() =>
		_restClient.GetAsync<TimesheetRemindersResponse>("/api/timesheets/reminders", requireAuth: true);

	public Task<ApiResponse<List<ActivityTagItem>>> GetActivityTagsAsync() =>
		_restClient.GetAsync<List<ActivityTagItem>>("/api/timesheets/activity-tags", requireAuth: true);

	public Task<ApiResponse<EmployeeAllocationsResponse>> GetMyAllocationsAsync(string? weekStart = null)
	{
		var path = string.IsNullOrWhiteSpace(weekStart)
			? "/api/allocations/my"
			: $"/api/allocations/my?week={weekStart}";
		return _restClient.GetAsync<EmployeeAllocationsResponse>(path, requireAuth: true);
	}

	public Task<ApiResponse<TimesheetSubmittedResponse>> SubmitTimesheetAsync(SubmitTimesheetRequest request) =>
		_restClient.PostAsync<TimesheetSubmittedResponse>("/api/timesheets", request, requireAuth: true);

	public Task<ApiResponse<List<TimesheetListItem>>> GetMyTimesheetsAsync() =>
		_restClient.GetAsync<List<TimesheetListItem>>("/api/timesheets/my", requireAuth: true);

	public Task<ApiResponse<TimesheetDetail>> GetMyTimesheetDetailAsync(long timesheetId) =>
		_restClient.GetAsync<TimesheetDetail>($"/api/timesheets/my/{timesheetId}", requireAuth: true);
}
