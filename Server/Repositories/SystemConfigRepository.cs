using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
	private readonly PrmDbContext _context;

	public SystemConfigRepository(PrmDbContext context)
	{
		_context = context;
	}

	public Task<string?> GetValueByKeyAsync(string configKey, CancellationToken cancellationToken = default)
	{
		return _context.SystemConfigurations
			.Where(config => config.ConfigKey == configKey)
			.Select(config => config.ConfigValue)
			.FirstOrDefaultAsync(cancellationToken);
	}
}
