namespace PRM.Server.Services.Ai;

public interface ILlmClientRegistration
{
	IReadOnlyCollection<string> ProviderNames { get; }
	ILlmClient Create();
}
