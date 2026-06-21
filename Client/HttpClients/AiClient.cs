using PRM.Client.Models;
using PRM.Client.Models.Manager;

namespace PRM.Client.HttpClients;

public class AiClient
{
	private readonly RestClient _restClient;

	public AiClient(RestClient restClient)
	{
		_restClient = restClient;
	}

	public Task<ApiResponse<AiSkillMatchResponse>> GetSkillMatchAsync(string requirement) =>
		_restClient.GetAsync<AiSkillMatchResponse>(
			$"/api/ai/skill-match?req={Uri.EscapeDataString(requirement)}",
			requireAuth: true);

	public Task<ApiResponse<AiRiskSummaryResponse>> GetRiskSummaryAsync(long projectId) =>
		_restClient.GetAsync<AiRiskSummaryResponse>($"/api/ai/risk-summary/{projectId}", requireAuth: true);

	public Task<ApiResponse<TeamBuilderResponse>> BuildTeamAsync(TeamBuilderRequest request) =>
		_restClient.PostAsync<TeamBuilderResponse>("/api/ai/team-builder", request, requireAuth: true);
}
