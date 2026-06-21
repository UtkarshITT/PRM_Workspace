using Microsoft.EntityFrameworkCore;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IUserService
{
	Task<UserCreatedDto> CreateUserAccountAsync(CreateUserDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
	Task ResetPasswordAsync(long userId, ResetPasswordDto dto, CancellationToken cancellationToken = default);
	Task UpdateRoleAsync(long userId, UpdateUserRoleDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task DeactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
	Task ReactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
	private readonly PrmDbContext _context;
	private readonly IUserRepository _userRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;

	public UserService(
		PrmDbContext context,
		IUserRepository userRepository,
		IResourceProfileRepository resourceProfileRepository)
	{
		_context = context;
		_userRepository = userRepository;
		_resourceProfileRepository = resourceProfileRepository;
	}

	public async Task<UserCreatedDto> CreateUserAccountAsync(
		CreateUserDto dto,
		long actorUserId,
		CancellationToken cancellationToken = default)
	{
		if (await _userRepository.GetByUsernameAsync(dto.Username, cancellationToken) != null)
		{
			throw new ConflictException($"Username '{dto.Username}' is already taken.");
		}

		if (await _userRepository.GetByEmailAsync(dto.Email, cancellationToken) != null)
		{
			throw new ConflictException($"Email '{dto.Email}' is already registered.");
		}

		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var now = DateTime.UtcNow;
			var user = new User
			{
				Username = dto.Username,
				Email = dto.Email,
				FullName = dto.FullName,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.TemporaryPassword),
				Role = dto.Role,
				IsActive = true,
				ForcePasswordChange = true,
				CreatedAt = now,
				UpdatedAt = now
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync(cancellationToken);

			var resourceProfile = new ResourceProfile
			{
				UserId = user.Id,
				ResourceProfileCode = $"RES-{user.Id:D6}",
				EmploymentStatus = "BENCH",
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			};

			_context.ResourceProfiles.Add(resourceProfile);

			_context.AuditLogs.Add(new AuditLog
			{
				ActorUserId = actorUserId,
				EntityName = "USERS",
				EntityId = user.Id,
				ActionType = "CREATE",
				NewValues = $"{{\"username\":\"{user.Username}\",\"role\":\"{user.Role}\"}}",
				CreatedAt = now
			});

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return new UserCreatedDto
			{
				UserId = user.Id,
				EmployeeId = resourceProfile.Id,
				EmployeeCode = resourceProfile.ResourceProfileCode
			};
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
	{
		var users = await _userRepository.GetAllAsync(cancellationToken);

		return users.Select(user => new UserListItemDto
		{
			Id = user.Id,
			Username = user.Username,
			FullName = user.FullName,
			Email = user.Email,
			Role = user.Role,
			IsActive = user.IsActive
		}).ToList();
	}

	public async Task ResetPasswordAsync(long userId, ResetPasswordDto dto, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

		if (user == null)
		{
			throw new NotFoundException($"User with ID {userId} was not found.");
		}

		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewTemporaryPassword);
		user.ForcePasswordChange = true;
		user.UpdatedAt = DateTime.UtcNow;
		await _userRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task UpdateRoleAsync(
		long userId,
		UpdateUserRoleDto dto,
		long actorUserId,
		CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

		if (user == null)
		{
			throw new NotFoundException($"User with ID {userId} was not found.");
		}

		if (!Roles.All.Contains(dto.Role))
		{
			throw new ValidationException($"Role must be one of: {string.Join(", ", Roles.All)}.");
		}

		if (user.Role == dto.Role)
		{
			throw new ValidationException("User already has the selected role.");
		}

		var oldRole = user.Role;
		user.Role = dto.Role;
		user.UpdatedAt = DateTime.UtcNow;

		_context.AuditLogs.Add(new AuditLog
		{
			ActorUserId = actorUserId,
			EntityName = "USERS",
			EntityId = user.Id,
			ActionType = "UPDATE_ROLE",
			OldValues = $"{{\"role\":\"{oldRole}\"}}",
			NewValues = $"{{\"role\":\"{user.Role}\"}}",
			CreatedAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task DeactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

		if (user == null)
		{
			throw new NotFoundException($"User with ID {userId} was not found.");
		}

		if (!user.IsActive)
		{
			throw new ValidationException("User account is already inactive.");
		}

		user.IsActive = false;
		user.UpdatedAt = DateTime.UtcNow;

		var resourceProfile = await _resourceProfileRepository.GetByUserIdAsync(userId, cancellationToken);
		if (resourceProfile != null)
		{
			resourceProfile.IsActive = false;
			resourceProfile.UpdatedAt = DateTime.UtcNow;
		}

		await _userRepository.SaveChangesAsync(cancellationToken);
	}

	public async Task ReactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

		if (user == null)
		{
			throw new NotFoundException($"User with ID {userId} was not found.");
		}

		if (user.IsActive)
		{
			throw new ValidationException("User account is already active.");
		}

		user.IsActive = true;
		user.UpdatedAt = DateTime.UtcNow;

		var resourceProfile = await _resourceProfileRepository.GetByUserIdAsync(userId, cancellationToken);
		if (resourceProfile != null)
		{
			resourceProfile.IsActive = true;
			resourceProfile.UpdatedAt = DateTime.UtcNow;
		}

		await _userRepository.SaveChangesAsync(cancellationToken);
	}
}
