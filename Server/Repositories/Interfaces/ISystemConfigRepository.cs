namespace PRM.Server.Repositories.Interfaces;

public interface ISystemConfigRepository
{
	Task<string?> GetValueByKeyAsync(string configKey, CancellationToken cancellationToken = default);
}
