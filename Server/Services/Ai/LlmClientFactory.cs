namespace PRM.Server.Services.Ai;

public interface ILlmClientFactory
{
	ILlmClient Create(string providerName);
}

public class LlmClientFactory : ILlmClientFactory
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public LlmClientFactory(
		IHttpClientFactory httpClientFactory,
		IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	public ILlmClient Create(string providerName)
	{
		return providerName.Trim().ToLowerInvariant() switch
		{
			"gemini" => new GeminiClient(_httpClientFactory.CreateClient("GeminiLlm"), _configuration),
			"groq" => new GroqClient(_httpClientFactory.CreateClient("GroqLlm"), _configuration),
			"custom" or "ollama" => new CustomGenerateClient(
				_httpClientFactory.CreateClient("CustomGenerateLlm"),
				_configuration),
			_ => throw new ArgumentException($"Unknown LLM provider: {providerName}")
		};
	}
}
