using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PRM.Server.Configuration;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Auth;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories;
using PRM.Server.Services;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class AuthServiceTests : IDisposable
{
	private readonly PrmDbContext _context;
	private readonly AuthService _authService;

	public AuthServiceTests()
	{
		var options = new DbContextOptionsBuilder<PrmDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		_context = new PrmDbContext(options);

		var jwtSettings = Options.Create(new JwtSettings
		{
			SecretKey = "TestSecretKey_MinimumThirtyTwoCharacters!",
			Issuer = "PRM.Server",
			Audience = "PRM.Client",
			ExpiryHours = 8
		});

		_authService = new AuthService(
			new UserRepository(_context),
			new EmployeeRepository(_context),
			new AuditLogRepository(_context),
			new TokenService(jwtSettings),
			NullLogger<AuthService>.Instance);
	}

	[Fact]
	public async Task LoginAsync_WithValidCredentials_ReturnsToken()
	{
		await SeedUserAsync("admin", "Admin@1234", forcePasswordChange: false);

		var result = await _authService.LoginAsync(new LoginDto
		{
			Username = "admin",
			Password = "Admin@1234"
		});

		result.Token.Should().NotBeNullOrWhiteSpace();
		result.Role.Should().Be("ADMIN");
		result.ForcePasswordChange.Should().BeFalse();
	}

	[Fact]
	public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorized()
	{
		await SeedUserAsync("admin", "Admin@1234");

		var act = () => _authService.LoginAsync(new LoginDto
		{
			Username = "admin",
			Password = "WrongPass1"
		});

		await act.Should().ThrowAsync<UnauthorizedAppException>();
	}

	[Fact]
	public async Task ChangePasswordAsync_WithValidInput_ClearsForcePasswordChange()
	{
		var user = await SeedUserAsync("admin", "Admin@1234", forcePasswordChange: true);

		var result = await _authService.ChangePasswordAsync(user.Id, new PasswordChangeDto
		{
			CurrentPassword = "Admin@1234",
			NewPassword = "NewPass123"
		});

		result.ForcePasswordChange.Should().BeFalse();
		result.Token.Should().NotBeNullOrWhiteSpace();

		var updatedUser = await _context.Users.FindAsync(user.Id);
		updatedUser!.ForcePasswordChange.Should().BeFalse();
	}

	[Fact]
	public async Task ChangePasswordAsync_WithWeakPassword_ThrowsValidationException()
	{
		var user = await SeedUserAsync("admin", "Admin@1234", forcePasswordChange: true);

		var act = () => _authService.ChangePasswordAsync(user.Id, new PasswordChangeDto
		{
			CurrentPassword = "Admin@1234",
			NewPassword = "weak"
		});

		await act.Should().ThrowAsync<ValidationException>();
	}

	[Fact]
	public void PasswordValidator_RejectsInvalidPasswords()
	{
		var errors = PasswordValidator.Validate("short");
		errors.Should().NotBeEmpty();
	}

	private async Task<User> SeedUserAsync(string username, string password, bool forcePasswordChange = false)
	{
		var now = DateTime.UtcNow;
		var user = new User
		{
			Username = username,
			Email = $"{username}@techserve.com",
			FullName = "Test User",
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
			Role = "ADMIN",
			IsActive = true,
			ForcePasswordChange = forcePasswordChange,
			CreatedAt = now,
			UpdatedAt = now
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_context.Employees.Add(new Employee
		{
			UserId = user.Id,
			EmployeeCode = $"EMP-{user.Id:D6}",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		});

		await _context.SaveChangesAsync();
		return user;
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}
