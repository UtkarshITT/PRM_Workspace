namespace PRM.Server.Repositories.Interfaces;

using PRM.Server.Models.Entities;
using PRM.Server.Services.Email;

public interface ISystemConfigRepository
{
	Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<string?> GetValueByKeyAsync(string configKey, CancellationToken cancellationToken = default);
	Task<int> GetSchedulerIntervalHoursAsync(CancellationToken cancellationToken = default);
	Task<(string Provider, string ApiKey)> GetLlmSettingsAsync(CancellationToken cancellationToken = default);
	Task<EmailSettings> GetEmailSettingsAsync(CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
