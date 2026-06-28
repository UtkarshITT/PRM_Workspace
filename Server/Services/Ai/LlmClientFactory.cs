namespace PRM.Server.Services.Ai;

public interface ILlmClientFactory
{
	ILlmClient Create(string providerName);
}

public class LlmClientFactory : ILlmClientFactory
{
	private readonly IReadOnlyDictionary<string, ILlmClientRegistration> _registrations;

	public LlmClientFactory(IEnumerable<ILlmClientRegistration> registrations)
	{
		_registrations = registrations
			.SelectMany(registration => registration.ProviderNames.Select(providerName => new
			{
				Name = providerName.Trim().ToLowerInvariant(),
				Registration = registration
			}))
			.ToDictionary(item => item.Name, item => item.Registration);
	}

	public ILlmClient Create(string providerName)
	{
		var normalizedProviderName = providerName.Trim().ToLowerInvariant();
		if (_registrations.TryGetValue(normalizedProviderName, out var registration))
		{
			return registration.Create();
		}

		throw new ArgumentException($"Unknown LLM provider: {providerName}");
	}
}
