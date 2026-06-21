using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Email;

namespace PRM.Tests.Services;

public class EmailNotificationServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly EmailNotificationService _service;

	public EmailNotificationServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_service = CreateService();
	}

	[Fact]
	public async Task SendAsync_WithBothChannelsEnabled_WritesConsoleAndSkipsSmtpWhenNotConfigured()
	{
		await SeedEmailConfigAsync(consoleEnabled: true, smtpEnabled: true);
		var userId = await SeedUserAsync();

		await _service.SendAsync(new NotificationEmailRequest
		{
			NotificationType = "TEST",
			RecipientUserId = userId,
			RecipientEmail = "user@test.com",
			Subject = "Hello",
			Body = "Test body"
		});

		var logs = await _context.NotificationLogs.OrderBy(log => log.DeliveryChannel).ToListAsync();
		logs.Should().HaveCount(2);
		logs[0].DeliveryChannel.Should().Be(EmailNotificationService.ChannelConsole);
		logs[0].Status.Should().Be("SENT");
		logs[1].DeliveryChannel.Should().Be(EmailNotificationService.ChannelSmtp);
		logs[1].Status.Should().Be("SKIPPED");
	}

	[Fact]
	public async Task SendAsync_WithOnlyConsoleEnabled_WritesSingleConsoleLog()
	{
		await SeedEmailConfigAsync(consoleEnabled: true, smtpEnabled: false);
		var userId = await SeedUserAsync();

		await _service.SendAsync(new NotificationEmailRequest
		{
			NotificationType = "TEST",
			RecipientUserId = userId,
			RecipientEmail = "user@test.com",
			Subject = "Hello",
			Body = "Test body"
		});

		var logs = await _context.NotificationLogs.ToListAsync();
		logs.Should().ContainSingle();
		logs[0].DeliveryChannel.Should().Be(EmailNotificationService.ChannelConsole);
	}

	[Fact]
	public async Task SendAsync_WithOnlySmtpEnabled_AttemptsSmtpOnly()
	{
		await SeedEmailConfigAsync(consoleEnabled: false, smtpEnabled: true);
		var service = CreateService(
			new Dictionary<string, string?>
			{
				["Email:Smtp:Host"] = "invalid.smtp.local",
				["Email:Smtp:Port"] = "587",
				["Email:Smtp:FromAddress"] = "noreply@test.com"
			});
		var userId = await SeedUserAsync();

		await service.SendAsync(new NotificationEmailRequest
		{
			NotificationType = "TEST",
			RecipientUserId = userId,
			RecipientEmail = "user@test.com",
			Subject = "Hello",
			Body = "Test body"
		});

		var logs = await _context.NotificationLogs.ToListAsync();
		logs.Should().ContainSingle();
		logs[0].DeliveryChannel.Should().Be(EmailNotificationService.ChannelSmtp);
		logs[0].Status.Should().Be("FAILED");
	}

	private EmailNotificationService CreateService(Dictionary<string, string?>? settings = null)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(settings ?? new Dictionary<string, string?>())
			.Build();

		return new EmailNotificationService(
			new SystemConfigRepository(_context, configuration),
			new NotificationLogRepository(_context),
			NullLogger<EmailNotificationService>.Instance);
	}

	private async Task SeedEmailConfigAsync(bool consoleEnabled, bool smtpEnabled)
	{
		var now = DateTime.UtcNow;
		_context.SystemConfigurations.AddRange(
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailConsoleEnabled, ConfigValue = consoleEnabled.ToString().ToLowerInvariant(), UpdatedAt = now },
			new SystemConfiguration { ConfigKey = SystemConfigKeys.EmailSmtpEnabled, ConfigValue = smtpEnabled.ToString().ToLowerInvariant(), UpdatedAt = now });
		await _context.SaveChangesAsync();
	}

	private async Task<long> SeedUserAsync()
	{
		var now = DateTime.UtcNow;
		var user = new User
		{
			Username = "email.user",
			Email = "user@test.com",
			FullName = "Email User",
			PasswordHash = "hash",
			Role = "EMPLOYEE",
			IsActive = true,
			ForcePasswordChange = false,
			CreatedAt = now,
			UpdatedAt = now
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();
		return user.Id;
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
