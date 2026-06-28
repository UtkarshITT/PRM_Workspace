using System.Net.Http.Json;
using System.Text.Json;

namespace PRM.Server.Services.Ai;

public class CustomGenerateClient : ILlmClient
{
	private const string DefaultModel = "gemma3:12b-it-q8_0";
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public CustomGenerateClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_configuration = configuration;
	}

	public async Task<string> GenerateResponseAsync(string prompt, string apiKey, CancellationToken cancellationToken = default)
	{
		var model = Environment.GetEnvironmentVariable("PRM_CUSTOM_LLM_MODEL");
		if (string.IsNullOrWhiteSpace(model))
		{
			model = _configuration["Llm:Custom:Model"];
		}

		if (string.IsNullOrWhiteSpace(model))
		{
			model = DefaultModel;
		}

		using var request = new HttpRequestMessage(HttpMethod.Post, BuildGenerateUri());
		request.Headers.Add("apikey", apiKey);
		request.Content = JsonContent.Create(new
		{
			model,
			prompt,
			stream = false
		});

		using var response = await _httpClient.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new HttpRequestException(
				$"Custom LLM request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Model: {model}. Response: {errorBody}");
		}

		var json = await response.Content.ReadFromJsonAsync<CustomGenerateResponse>(JsonOptions, cancellationToken);
		if (string.IsNullOrWhiteSpace(json?.Response))
		{
			throw new InvalidOperationException("Custom LLM returned an empty response.");
		}

		return json.Response;
	}

	private Uri BuildGenerateUri()
	{
		if (_httpClient.BaseAddress == null)
		{
			throw new InvalidOperationException("Custom LLM base URL is not configured.");
		}

		if (_httpClient.BaseAddress.Scheme == Uri.UriSchemeHttp && !_httpClient.BaseAddress.IsLoopback)
		{
			throw new InvalidOperationException("Custom LLM base URL must use HTTPS unless it points to localhost.");
		}

		var path = _httpClient.BaseAddress.AbsolutePath.TrimEnd('/');
		return path.EndsWith("/api/generate", StringComparison.OrdinalIgnoreCase)
			? _httpClient.BaseAddress
			: new Uri(_httpClient.BaseAddress, "api/generate");
	}

	private sealed class CustomGenerateResponse
	{
		public string? Response { get; set; }
	}
}
