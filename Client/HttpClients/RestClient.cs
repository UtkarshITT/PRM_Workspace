using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PRM.Client.Helpers;
using PRM.Client.Models;

namespace PRM.Client.HttpClients;

public class RestClient
{
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;

	public RestClient(string baseUrl)
	{
		_baseUrl = baseUrl.TrimEnd('/');

		var handler = new HttpClientHandler();
		handler.ServerCertificateCustomValidationCallback =
			HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

		_httpClient = new HttpClient(handler);
	}

	public Task<ApiResponse<T>> GetAsync<T>(string path, bool requireAuth = false) =>
		SendAsync<T>(() => CreateRequest(HttpMethod.Get, path, requireAuth));

	public async Task<ApiResponse<T>> PostAsync<T>(string path, object body, bool requireAuth = false)
	{
		return await SendAsync<T>(() =>
		{
			var request = CreateRequest(HttpMethod.Post, path, requireAuth);
			request.Content = JsonContent.Create(body);
			return request;
		});
	}

	public async Task<ApiResponse<T>> PutAsync<T>(string path, object body, bool requireAuth = false)
	{
		return await SendAsync<T>(() =>
		{
			var request = CreateRequest(HttpMethod.Put, path, requireAuth);
			request.Content = JsonContent.Create(body);
			return request;
		});
	}

	public Task<ApiResponse<T>> DeleteAsync<T>(string path, bool requireAuth = false) =>
		SendAsync<T>(() => CreateRequest(HttpMethod.Delete, path, requireAuth));

	private async Task<ApiResponse<T>> SendAsync<T>(Func<HttpRequestMessage> createRequest)
	{
		try
		{
			using var request = createRequest();
			using var response = await _httpClient.SendAsync(request);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				SessionStore.Clear();
				return new ApiResponse<T>
				{
					Success = false,
					Error = "Session expired. Please log in again."
				};
			}

			var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();

			if (payload == null)
			{
				return new ApiResponse<T>
				{
					Success = false,
					Error = $"Unexpected response from server ({(int)response.StatusCode})."
				};
			}

			return payload;
		}
		catch (HttpRequestException)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Error = $"Cannot reach server at {_baseUrl}. Start the server first: dotnet run --project Server/PRM.Server.csproj"
			};
		}
	}

	private HttpRequestMessage CreateRequest(HttpMethod method, string path, bool requireAuth)
	{
		var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");

		if (requireAuth && SessionStore.IsAuthenticated)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionStore.Token);
		}

		return request;
	}
}
