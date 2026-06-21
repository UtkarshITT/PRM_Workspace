using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Auth;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IAuthService
{
	Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
	Task<LoginResponseDto> ChangePasswordAsync(long userId, PasswordChangeDto dto, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
	private readonly IUserRepository _userRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly ITokenService _tokenService;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		IUserRepository userRepository,
		IResourceProfileRepository resourceProfileRepository,
		IAuditLogRepository auditLogRepository,
		ITokenService tokenService,
		ILogger<AuthService> logger)
	{
		_userRepository = userRepository;
		_resourceProfileRepository = resourceProfileRepository;
		_auditLogRepository = auditLogRepository;
		_tokenService = tokenService;
		_logger = logger;
	}

	public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByUsernameAsync(dto.Username, cancellationToken);

		if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
		{
			_logger.LogWarning("Failed login attempt for username {Username}", dto.Username);
			throw new UnauthorizedAppException("Invalid username or password.");
		}

		var resourceProfile = await _resourceProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
		var (token, expiresAt) = _tokenService.GenerateToken(user, resourceProfile);

		user.LastLoginAt = DateTime.UtcNow;
		user.UpdatedAt = DateTime.UtcNow;
		await _userRepository.SaveChangesAsync(cancellationToken);

		await _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = user.Id,
			EntityName = "USERS",
			EntityId = user.Id,
			ActionType = "LOGIN",
			NewValues = $"{{\"username\":\"{user.Username}\"}}",
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);

		return new LoginResponseDto
		{
			Token = token,
			ExpiresAt = expiresAt,
			UserId = user.Id,
			ResourceProfileId = resourceProfile?.Id,
			Role = user.Role,
			FullName = user.FullName,
			ForcePasswordChange = user.ForcePasswordChange
		};
	}

	public async Task<LoginResponseDto> ChangePasswordAsync(long userId, PasswordChangeDto dto, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

		if (user == null || !user.IsActive)
		{
			throw new UnauthorizedAppException("User account is not available.");
		}

		if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
		{
			throw new ValidationException("Current password is incorrect.");
		}

		var passwordErrors = PasswordValidator.Validate(dto.NewPassword);
		if (passwordErrors.Count > 0)
		{
			throw new ValidationException("Password does not meet requirements.", passwordErrors);
		}

		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
		user.ForcePasswordChange = false;
		user.UpdatedAt = DateTime.UtcNow;
		await _userRepository.SaveChangesAsync(cancellationToken);

		var resourceProfile = await _resourceProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
		var (token, expiresAt) = _tokenService.GenerateToken(user, resourceProfile);

		return new LoginResponseDto
		{
			Token = token,
			ExpiresAt = expiresAt,
			UserId = user.Id,
			ResourceProfileId = resourceProfile?.Id,
			Role = user.Role,
			FullName = user.FullName,
			ForcePasswordChange = false
		};
	}
}
