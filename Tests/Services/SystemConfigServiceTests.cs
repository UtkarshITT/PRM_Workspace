using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.SystemConfig;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class SystemConfigServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly IDataProtectionProvider _dataProtectionProvider;
	private readonly SystemConfigRepository _repository;
	private readonly SystemConfigService _service;

	public SystemConfigServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(
			Path.Combine(Path.GetTempPath(), $"prm-tests-{Guid.NewGuid():N}")));
		_repository = new SystemConfigRepository(_context, dataProtectionProvider: _dataProtectionProvider);
		_service = new SystemConfigService(
			_repository,
			new AuditLogRepository(_context),
			_dataProtectionProvider);
	}

	[Fact]
	public async Task GetAllAsync_WithConfiguredLlmKey_MasksSecretValue()
	{
		await SeedConfigAsync(llmApiKey: "plain-existing-key");

		var result = await _service.GetAllAsync();

		var keyConfig = result.Single(config => config.Key == SystemConfigKeys.LlmApiKey);
		keyConfig.Value.Should().Be("****");
		keyConfig.IsSecret.Should().BeTrue();
		keyConfig.IsConfigured.Should().BeTrue();
	}

	[Fact]
	public async Task UpdateAsync_WithLlmApiKey_EncryptsAtRestAndRepositoryReturnsPlaintext()
	{
		await SeedConfigAsync();

		await _service.UpdateAsync(new UpdateSystemConfigDto
		{
			LlmProvider = "Custom",
			LlmApiKey = "secret-key",
			SchedulerIntervalHours = 2,
			MaxWeeklyHours = 45
		}, adminUserId: 10);

		var storedKey = await _context.SystemConfigurations
			.Where(config => config.ConfigKey == SystemConfigKeys.LlmApiKey)
			.Select(config => config.ConfigValue)
			.SingleAsync();
		storedKey.Should().StartWith("dp:");
		storedKey.Should().NotContain("secret-key");

		var (provider, apiKey) = await _repository.GetLlmSettingsAsync();
		provider.Should().Be("Custom");
		apiKey.Should().Be("secret-key");
		(await _repository.GetSchedulerIntervalHoursAsync()).Should().Be(2);

		var audit = await _context.AuditLogs.SingleAsync();
		audit.NewValues.Should().Contain("***REDACTED***");
	}

	[Fact]
	public async Task GetAllAsync_HidesEmailChannelFlagsFromAdminConfig()
	{
		await SeedConfigAsync();

		var result = await _service.GetAllAsync();

		result.Select(config => config.Key).Should().Equal(
			SystemConfigKeys.LlmProvider,
			SystemConfigKeys.LlmApiKey,
			SystemConfigKeys.SchedulerIntervalHours,
			SystemConfigKeys.MaxWeeklyHours);
	}

	[Fact]
	public async Task UpdateAsync_WithInvalidProvider_ThrowsValidation()
	{
		await SeedConfigAsync();

		var act = () => _service.UpdateAsync(new UpdateSystemConfigDto
		{
			LlmProvider = "OpenAI"
		}, adminUserId: 10);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*Gemini, Groq, Custom, or Ollama*");
	}

	private async Task SeedConfigAsync(string llmApiKey = "")
	{
		var now = DateTime.UtcNow;
		_context.SystemConfigurations.AddRange(
			new SystemConfiguration { ConfigKey = SystemConfigKeys.LlmProvider, ConfigValue = "Gemini", Description = "Active LLM provider", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.LlmApiKey, ConfigValue = llmApiKey, Description = "LLM API key", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.SchedulerIntervalHours, ConfigValue = "4", Description = "Scheduler interval", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.MaxWeeklyHours, ConfigValue = "40", Description = "Max hours", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailConsoleEnabled, ConfigValue = "true", Description = "Console email", UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailSmtpEnabled, ConfigValue = "true", Description = "SMTP email", UpdatedAt = now });
		await _context.SaveChangesAsync();
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
