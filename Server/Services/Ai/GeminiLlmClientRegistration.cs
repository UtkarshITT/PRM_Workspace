namespace PRM.Server.Services.Ai;

public class GeminiLlmClientRegistration : ILlmClientRegistration
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public GeminiLlmClientRegistration(IHttpClientFactory httpClientFactory, IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	public IReadOnlyCollection<string> ProviderNames { get; } = ["gemini"];

	public ILlmClient Create()
	{
		return new GeminiClient(_httpClientFactory.CreateClient("GeminiLlm"), _configuration);
	}
}
