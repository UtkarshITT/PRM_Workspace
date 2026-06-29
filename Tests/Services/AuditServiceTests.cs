using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Middleware;
using PRM.Server.Models.DTOs.Audit;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class AuditServiceTests : IDisposable
{
	private readonly PrmDbContext _context;

	public AuditServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		_context = new PrmDbContext(options);
	}

	[Fact]
	public async Task GetLogsAsync_ReturnsPagedResultsWithTotalCount()
	{
		await SeedActorAsync();
		await SeedAuditLogsAsync();
		var service = CreateService();

		var result = await service.GetLogsAsync(new AuditLogFilterDto { Page = 2, PageSize = 2 });

		result.TotalCount.Should().Be(5);
		result.Page.Should().Be(2);
		result.PageSize.Should().Be(2);
		result.TotalPages.Should().Be(3);
		result.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetLogsAsync_AppliesFilters()
	{
		await SeedActorAsync();
		await SeedAuditLogsAsync();
		var service = CreateService();

		var result = await service.GetLogsAsync(new AuditLogFilterDto
		{
			ActorUserId = 1,
			ActionType = "UPDATE",
			EntityName = "PROJECTS",
			EntityId = 20,
			From = new DateTime(2026, 6, 2),
			To = new DateTime(2026, 6, 4),
			PageSize = 10
		});

		result.Items.Should().ContainSingle();
		result.Items[0].ActionType.Should().Be("UPDATE");
		result.Items[0].EntityName.Should().Be("PROJECTS");
		result.Items[0].EntityId.Should().Be(20);
	}

	[Fact]
	public async Task LogAsync_WritesCorrelationIdFromHttpContext()
	{
		await SeedActorAsync();
		var httpContextAccessor = new HttpContextAccessor
		{
			HttpContext = new DefaultHttpContext()
		};
		httpContextAccessor.HttpContext.Items[CorrelationIdMiddleware.ItemName] = "corr-123";
		var service = new AuditService(new AuditLogRepository(_context, httpContextAccessor));

		await service.LogAsync(1, "CREATE", "USERS", 99, "Created test user");

		var audit = await _context.AuditLogs.SingleAsync();
		audit.CorrelationId.Should().Be("corr-123");
		audit.ActionType.Should().Be("CREATE");
		audit.NewValues.Should().Contain("Created test user");
	}

	private AuditService CreateService()
	{
		return new AuditService(new AuditLogRepository(_context));
	}

	private async Task SeedActorAsync()
	{
		_context.Users.Add(new User
		{
			Id = 1,
			Username = "admin",
			Email = "admin@test.com",
			FullName = "Admin User",
			PasswordHash = "hash",
			Role = "ADMIN",
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();
	}

	private async Task SeedAuditLogsAsync()
	{
		var rows = new[]
		{
			CreateAuditLog(1, "CREATE", "USERS", 10, new DateTime(2026, 6, 1)),
			CreateAuditLog(1, "UPDATE", "PROJECTS", 20, new DateTime(2026, 6, 2)),
			CreateAuditLog(1, "UPDATE", "PROJECTS", 21, new DateTime(2026, 6, 3)),
			CreateAuditLog(1, "END", "PROJECT_ALLOCATIONS", 30, new DateTime(2026, 6, 4)),
			CreateAuditLog(1, "SUBMIT", "TIMESHEETS", 40, new DateTime(2026, 6, 5))
		};

		_context.AuditLogs.AddRange(rows);
		await _context.SaveChangesAsync();
	}

	private static AuditLog CreateAuditLog(long actorUserId, string actionType, string entityName, long entityId, DateTime createdAt)
	{
		return new AuditLog
		{
			ActorUserId = actorUserId,
			ActionType = actionType,
			EntityName = entityName,
			EntityId = entityId,
			CreatedAt = createdAt
		};
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
