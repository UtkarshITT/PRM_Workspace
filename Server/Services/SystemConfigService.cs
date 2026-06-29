using Microsoft.AspNetCore.DataProtection;
using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.SystemConfig;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface ISystemConfigService
{
	Task<IReadOnlyList<SystemConfigItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SystemConfigItemDto>> UpdateAsync(
		UpdateSystemConfigDto dto,
		long adminUserId,
		CancellationToken cancellationToken = default);
}

public class SystemConfigService : ISystemConfigService
{
	private const string LlmApiKeyProtectorPurpose = "PRM.SystemConfigurations.LlmApiKey.v1";
	private const string ProtectedPrefix = "dp:";
	private static readonly string[] DisplayOrder =
	[
		SystemConfigKeys.LlmProvider,
		SystemConfigKeys.LlmApiKey,
		SystemConfigKeys.SchedulerIntervalHours,
		SystemConfigKeys.MaxWeeklyHours
	];

	private readonly ISystemConfigRepository _systemConfigRepository;
	private readonly IAuditService _auditService;
	private readonly IDataProtector _llmApiKeyProtector;

	public SystemConfigService(
		ISystemConfigRepository systemConfigRepository,
		IAuditService auditService,
		IDataProtectionProvider dataProtectionProvider)
	{
		_systemConfigRepository = systemConfigRepository;
		_auditService = auditService;
		_llmApiKeyProtector = dataProtectionProvider.CreateProtector(LlmApiKeyProtectorPurpose);
	}

	public async Task<IReadOnlyList<SystemConfigItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);
		return ToDtos(configs);
	}

	public async Task<IReadOnlyList<SystemConfigItemDto>> UpdateAsync(
		UpdateSystemConfigDto dto,
		long adminUserId,
		CancellationToken cancellationToken = default)
	{
		var configs = (await _systemConfigRepository.GetAllAsync(cancellationToken)).ToDictionary(
			config => config.ConfigKey,
			StringComparer.OrdinalIgnoreCase);
		var now = DateTime.UtcNow;
		var changedKeys = new List<string>();

		if (dto.LlmProvider != null)
		{
			var provider = dto.LlmProvider.Trim();
			if (!provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
				&& !provider.Equals("Groq", StringComparison.OrdinalIgnoreCase)
				&& !provider.Equals("Custom", StringComparison.OrdinalIgnoreCase)
				&& !provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
			{
				throw new ValidationException("LLM provider must be Gemini, Groq, Custom, or Ollama.");
			}

			UpdateValue(configs, SystemConfigKeys.LlmProvider, ToCanonicalProvider(provider), adminUserId, now, changedKeys);
		}

		if (dto.LlmApiKey != null)
		{
			var protectedKey = string.IsNullOrWhiteSpace(dto.LlmApiKey)
				? string.Empty
				: ProtectedPrefix + _llmApiKeyProtector.Protect(dto.LlmApiKey.Trim());
			UpdateValue(configs, SystemConfigKeys.LlmApiKey, protectedKey, adminUserId, now, changedKeys);
		}

		if (dto.SchedulerIntervalHours.HasValue)
		{
			ValidatePositive(dto.SchedulerIntervalHours.Value, "Scheduler interval hours");
			UpdateValue(
				configs,
				SystemConfigKeys.SchedulerIntervalHours,
				dto.SchedulerIntervalHours.Value.ToString(),
				adminUserId,
				now,
				changedKeys);
		}

		if (dto.MaxWeeklyHours.HasValue)
		{
			ValidatePositive(dto.MaxWeeklyHours.Value, "Max weekly hours");
			UpdateValue(
				configs,
				SystemConfigKeys.MaxWeeklyHours,
				dto.MaxWeeklyHours.Value.ToString(),
				adminUserId,
				now,
				changedKeys);
		}

		if (changedKeys.Count > 0)
		{
			await _systemConfigRepository.SaveChangesAsync(cancellationToken);
			await _auditService.LogAsync(
				adminUserId,
				"UPDATE",
				"SYSTEM_CONFIGURATIONS",
				0,
				"System configuration updated",
				newValues: BuildAuditJson(changedKeys),
				cancellationToken: cancellationToken);
		}

		return ToDtos(configs.Values);
	}

	private static void UpdateValue(
		IReadOnlyDictionary<string, SystemConfiguration> configs,
		string key,
		string value,
		long adminUserId,
		DateTime now,
		ICollection<string> changedKeys)
	{
		if (!configs.TryGetValue(key, out var config))
		{
			throw new ValidationException($"System configuration key '{key}' is not seeded.");
		}

		if (config.ConfigValue == value)
		{
			return;
		}

		config.ConfigValue = value;
		config.UpdatedAt = now;
		config.UpdatedByUserId = adminUserId;
		changedKeys.Add(key);
	}

	private static IReadOnlyList<SystemConfigItemDto> ToDtos(IEnumerable<SystemConfiguration> configs)
	{
		var byKey = configs.ToDictionary(config => config.ConfigKey, StringComparer.OrdinalIgnoreCase);
		return DisplayOrder
			.Where(byKey.ContainsKey)
			.Select(key => ToDto(byKey[key]))
			.ToList();
	}

	private static SystemConfigItemDto ToDto(SystemConfiguration config)
	{
		var isSecret = config.ConfigKey.Equals(SystemConfigKeys.LlmApiKey, StringComparison.OrdinalIgnoreCase);
		var envLlmKeyConfigured = isSecret && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PRM_LLM_API_KEY"));
		var isConfigured = isSecret
			? !string.IsNullOrWhiteSpace(config.ConfigValue) || envLlmKeyConfigured
			: !string.IsNullOrWhiteSpace(config.ConfigValue);

		return new SystemConfigItemDto
		{
			Key = config.ConfigKey,
			Value = isSecret && isConfigured ? "****" : config.ConfigValue,
			Description = config.Description,
			IsSecret = isSecret,
			IsConfigured = isConfigured,
			UpdatedAt = config.UpdatedAt
		};
	}

	private static void ValidatePositive(int value, string fieldName)
	{
		if (value <= 0)
		{
			throw new ValidationException($"{fieldName} must be greater than zero.");
		}
	}

	private static string ToCanonicalProvider(string provider)
	{
		if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
		{
			return "Groq";
		}

		return provider.Equals("Custom", StringComparison.OrdinalIgnoreCase)
		       || provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
			? "Custom"
			: "Gemini";
	}

	private static string BuildAuditJson(IEnumerable<string> changedKeys)
	{
		var entries = changedKeys
			.Select(key => key == SystemConfigKeys.LlmApiKey ? "\"llm_api_key\":\"***REDACTED***\"" : $"\"{key}\":\"UPDATED\"");
		return "{" + string.Join(",", entries) + "}";
	}
}
