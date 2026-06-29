using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class UserServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly UserService _userService;

	public UserServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new PrmDbContext(options);
		_userService = new UserService(
			new UserRepository(_context),
			new ResourceProfileRepository(_context),
			new AuditService(new AuditLogRepository(_context)));
	}

	[Fact]
	public async Task CreateUserAccountAsync_CreatesUserAndEmployeeAtomically()
	{
		await SeedAdminAsync();

		var result = await _userService.CreateUserAccountAsync(new CreateUserDto
		{
			FullName = "Ravi Kumar",
			Email = "ravi@techserve.com",
			Username = "ravi.kumar",
			TemporaryPassword = "Welcome1",
			Role = "EMPLOYEE"
		}, actorUserId: 1);

		result.UserId.Should().BeGreaterThan(0);
		result.EmployeeId.Should().BeGreaterThan(0);
		result.EmployeeCode.Should().Be($"RES-{result.UserId:D6}");

		var resourceProfile = await _context.ResourceProfiles.FirstOrDefaultAsync(item => item.UserId == result.UserId);
		resourceProfile.Should().NotBeNull();
		resourceProfile!.EmploymentStatus.Should().Be("BENCH");

		var audit = await _context.AuditLogs.FirstOrDefaultAsync(item => item.EntityId == result.UserId);
		audit.Should().NotBeNull();
		audit!.ActionType.Should().Be("CREATE");
	}

	[Fact]
	public async Task CreateUserAccountAsync_WithDuplicateUsername_ThrowsConflict()
	{
		await SeedAdminAsync();
		var dto = new CreateUserDto
		{
			FullName = "Ravi Kumar",
			Email = "ravi@techserve.com",
			Username = "ravi.kumar",
			TemporaryPassword = "Welcome1",
			Role = "EMPLOYEE"
		};

		await _userService.CreateUserAccountAsync(dto, 1);

		var duplicateDto = new CreateUserDto
		{
			FullName = dto.FullName,
			Email = "other@techserve.com",
			Username = dto.Username,
			TemporaryPassword = dto.TemporaryPassword,
			Role = dto.Role
		};
		var act = () => _userService.CreateUserAccountAsync(duplicateDto, 1);
		await act.Should().ThrowAsync<ConflictException>();
	}

	private async Task SeedAdminAsync()
	{
		var now = DateTime.UtcNow;
		var admin = new User
		{
			Username = "admin",
			Email = "admin@techserve.com",
			FullName = "Admin",
			PasswordHash = "hash",
			Role = "ADMIN",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.Add(admin);
		await _context.SaveChangesAsync();
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
