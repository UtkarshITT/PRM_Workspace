using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
		SendAsync<T>(() => CreateRequest(HttpMethod.Get, path, requireAuth), requireAuth);

	public async Task<ApiResponse<T>> PostAsync<T>(string path, object body, bool requireAuth = false)
	{
		return await SendAsync<T>(() =>
		{
			var request = CreateRequest(HttpMethod.Post, path, requireAuth);
			request.Content = JsonContent.Create(body);
			return request;
		}, requireAuth);
	}

	public async Task<ApiResponse<T>> PutAsync<T>(string path, object body, bool requireAuth = false)
	{
		return await SendAsync<T>(() =>
		{
			var request = CreateRequest(HttpMethod.Put, path, requireAuth);
			request.Content = JsonContent.Create(body);
			return request;
		}, requireAuth);
	}

	public Task<ApiResponse<T>> DeleteAsync<T>(string path, bool requireAuth = false) =>
		SendAsync<T>(() => CreateRequest(HttpMethod.Delete, path, requireAuth), requireAuth);

	private async Task<ApiResponse<T>> SendAsync<T>(Func<HttpRequestMessage> createRequest, bool requireAuth)
	{
		try
		{
			using var request = createRequest();
			using var response = await _httpClient.SendAsync(request);
			var payload = await TryReadApiResponseAsync<T>(response);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				if (!requireAuth)
				{
					return payload ?? new ApiResponse<T>
					{
						Success = false,
						Error = "Invalid username or password."
					};
				}

				SessionStore.Clear();
				return new ApiResponse<T>
				{
					Success = false,
					Error = "Session expired. Please log in again."
				};
			}

			if (response.StatusCode == HttpStatusCode.Forbidden)
			{
				return new ApiResponse<T>
				{
					Success = false,
					Error = "You do not have permission to perform this action."
				};
			}

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
		request.Headers.TryAddWithoutValidation("X-Correlation-Id", Guid.NewGuid().ToString("N"));

		if (requireAuth && SessionStore.IsAuthenticated)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionStore.Token);
		}

		return request;
	}

	private static async Task<ApiResponse<T>?> TryReadApiResponseAsync<T>(HttpResponseMessage response)
	{
		try
		{
			return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
