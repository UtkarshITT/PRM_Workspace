namespace PRM.Server.Services.Ai;

public class GroqLlmClientRegistration : ILlmClientRegistration
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public GroqLlmClientRegistration(IHttpClientFactory httpClientFactory, IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	public IReadOnlyCollection<string> ProviderNames { get; } = ["groq"];

	public ILlmClient Create()
	{
		return new GroqClient(_httpClientFactory.CreateClient("GroqLlm"), _configuration);
	}
}
