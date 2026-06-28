namespace PRM.Server.Services.Ai;

public class CustomLlmClientRegistration : ILlmClientRegistration
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public CustomLlmClientRegistration(IHttpClientFactory httpClientFactory, IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	public IReadOnlyCollection<string> ProviderNames { get; } = ["custom", "ollama"];

	public ILlmClient Create()
	{
		return new CustomGenerateClient(_httpClientFactory.CreateClient("CustomGenerateLlm"), _configuration);
	}
}
