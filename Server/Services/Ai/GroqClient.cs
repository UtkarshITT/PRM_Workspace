using System.Net.Http.Json;
using System.Text.Json;

namespace PRM.Server.Services.Ai;

public class GroqClient : ILlmClient
{
	private const string DefaultModel = "llama-3.1-8b-instant";
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public GroqClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_configuration = configuration;
	}

	public async Task<string> GenerateResponseAsync(string prompt, string apiKey, CancellationToken cancellationToken = default)
	{
		var model = Environment.GetEnvironmentVariable("PRM_GROQ_MODEL");
		if (string.IsNullOrWhiteSpace(model))
		{
			model = _configuration["Llm:Groq:Model"];
		}

		if (string.IsNullOrWhiteSpace(model))
		{
			model = DefaultModel;
		}

		using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions");
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
		request.Content = JsonContent.Create(new
		{
			model,
			messages = new[] { new { role = "user", content = prompt } },
			temperature = 0.3,
			max_tokens = 800
		});

		using var response = await _httpClient.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new HttpRequestException(
				$"Groq request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Model: {model}. Response: {errorBody}");
		}

		var json = await response.Content.ReadFromJsonAsync<GroqResponse>(JsonOptions, cancellationToken);
		var text = json?.Choices?.FirstOrDefault()?.Message?.Content;

		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Groq returned an empty response.");
		}

		return text;
	}

	private sealed class GroqResponse
	{
		public List<GroqChoice>? Choices { get; set; }
	}

	private sealed class GroqChoice
	{
		public GroqMessage? Message { get; set; }
	}

	private sealed class GroqMessage
	{
		public string? Content { get; set; }
	}
}
