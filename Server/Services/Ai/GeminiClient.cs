using System.Net.Http.Json;
using System.Text.Json;

namespace PRM.Server.Services.Ai;

public class GeminiClient : ILlmClient
{
	private const string DefaultModel = "gemini-2.0-flash";
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public GeminiClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_configuration = configuration;
	}

	public async Task<string> GenerateResponseAsync(string prompt, string apiKey, CancellationToken cancellationToken = default)
	{
		var model = Environment.GetEnvironmentVariable("PRM_GEMINI_MODEL");
		if (string.IsNullOrWhiteSpace(model))
		{
			model = _configuration["Llm:Gemini:Model"];
		}

		if (string.IsNullOrWhiteSpace(model))
		{
			model = DefaultModel;
		}

		var url = $"v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
		var body = new
		{
			contents = new[]
			{
				new { parts = new[] { new { text = prompt } } }
			}
		};

		using var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new HttpRequestException(
				$"Gemini request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Response: {errorBody}");
		}

		var json = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
		var text = json?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Gemini returned an empty response.");
		}

		return text;
	}

	private sealed class GeminiResponse
	{
		public List<GeminiCandidate>? Candidates { get; set; }
	}

	private sealed class GeminiCandidate
	{
		public GeminiContent? Content { get; set; }
	}

	private sealed class GeminiContent
	{
		public List<GeminiPart>? Parts { get; set; }
	}

	private sealed class GeminiPart
	{
		public string? Text { get; set; }
	}
}
