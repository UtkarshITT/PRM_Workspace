using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Email;

namespace PRM.Server.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
	private const string LlmApiKeyProtectorPurpose = "PRM.SystemConfigurations.LlmApiKey.v1";
	private readonly PrmDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly IDataProtector? _llmApiKeyProtector;

	public SystemConfigRepository(
		PrmDbContext context,
		IConfiguration? configuration = null,
		IDataProtectionProvider? dataProtectionProvider = null)
	{
		_context = context;
		_configuration = configuration ?? new ConfigurationBuilder().Build();
		_llmApiKeyProtector = dataProtectionProvider?.CreateProtector(LlmApiKeyProtectorPurpose);
	}

	public async Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.SystemConfigurations
			.OrderBy(config => config.ConfigKey)
			.ToListAsync(cancellationToken);
	}

	public Task<string?> GetValueByKeyAsync(string configKey, CancellationToken cancellationToken = default)
	{
		return _context.SystemConfigurations
			.Where(config => config.ConfigKey == configKey)
			.Select(config => config.ConfigValue)
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<int> GetSchedulerIntervalHoursAsync(CancellationToken cancellationToken = default)
	{
		const int defaultIntervalHours = 4;
		var value = await GetValueByKeyAsync(SystemConfigKeys.SchedulerIntervalHours, cancellationToken);

		if (int.TryParse(value, out var hours) && hours > 0)
		{
			return hours;
		}

		return defaultIntervalHours;
	}

	public async Task<(string Provider, string ApiKey)> GetLlmSettingsAsync(CancellationToken cancellationToken = default)
	{
		var provider = await GetValueByKeyAsync(SystemConfigKeys.LlmProvider, cancellationToken);
		var apiKey = UnprotectLlmApiKey(await GetValueByKeyAsync(SystemConfigKeys.LlmApiKey, cancellationToken));

		var envKey = Environment.GetEnvironmentVariable("PRM_LLM_API_KEY");
		if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(envKey))
		{
			apiKey = envKey;
		}

		return (string.IsNullOrWhiteSpace(provider) ? "Gemini" : provider, apiKey ?? string.Empty);
	}

	public async Task<EmailSettings> GetEmailSettingsAsync(CancellationToken cancellationToken = default)
	{
		var consoleEnabled = await GetBoolAsync(SystemConfigKeys.EmailConsoleEnabled, defaultValue: true, cancellationToken);
		var smtpEnabled = await GetBoolAsync(SystemConfigKeys.EmailSmtpEnabled, defaultValue: true, cancellationToken);
		var portValue = GetSmtpSetting("Port");
		var port = int.TryParse(portValue, out var parsedPort) && parsedPort > 0 ? parsedPort : 587;

		return new EmailSettings
		{
			ConsoleEnabled = consoleEnabled,
			SmtpEnabled = smtpEnabled,
			SmtpHost = GetSmtpSetting("Host"),
			SmtpPort = port,
			SmtpUsername = GetSmtpSetting("Username"),
			SmtpPassword = GetSmtpSetting("Password"),
			SmtpFromAddress = GetSmtpSetting("FromAddress")
		};
	}

	private string GetSmtpSetting(string name)
	{
		var envValue = Environment.GetEnvironmentVariable($"PRM_SMTP_{ToEnvironmentName(name)}");
		if (!string.IsNullOrWhiteSpace(envValue))
		{
			return envValue;
		}

		return _configuration[$"Email:Smtp:{name}"] ?? string.Empty;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return _context.SaveChangesAsync(cancellationToken);
	}

	private static string ToEnvironmentName(string name)
	{
		return string.Concat(name.Select((character, index) =>
			index > 0 && char.IsUpper(character) ? $"_{character}" : character.ToString())).ToUpperInvariant();
	}

	private async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken cancellationToken)
	{
		var value = await GetValueByKeyAsync(key, cancellationToken);

		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"true" or "1" or "yes" or "on" => true,
			"false" or "0" or "no" or "off" => false,
			_ => defaultValue
		};
	}

	private string? UnprotectLlmApiKey(string? value)
	{
		if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("dp:", StringComparison.Ordinal))
		{
			return value;
		}

		if (_llmApiKeyProtector == null)
		{
			return string.Empty;
		}

		try
		{
			return _llmApiKeyProtector.Unprotect(value[3..]);
		}
		catch (CryptographicException)
		{
			return string.Empty;
		}
	}
}
