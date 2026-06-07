using PRM.Client.Models;
using PRM.Client.Models.Auth;

namespace PRM.Client.HttpClients;

public class AuthClient
{
	private readonly RestClient _restClient;

	public AuthClient(RestClient restClient)
	{
		_restClient = restClient;
	}

	public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
	{
		return _restClient.PostAsync<LoginResponse>("/api/auth/login", request);
	}

	public Task<ApiResponse<LoginResponse>> ChangePasswordAsync(PasswordChangeRequest request)
	{
		return _restClient.PostAsync<LoginResponse>("/api/auth/change-password", request, requireAuth: true);
	}
}
