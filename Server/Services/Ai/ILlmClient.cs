namespace PRM.Server.Services.Ai;

public interface ILlmClient
{
	Task<string> GenerateResponseAsync(string prompt, string apiKey, CancellationToken cancellationToken = default);
}
