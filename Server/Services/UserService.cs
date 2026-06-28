using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IUserService
{
	Task<UserCreatedDto> CreateUserAccountAsync(CreateUserDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default);
	Task ResetPasswordAsync(long userId, ResetPasswordDto dto, CancellationToken cancellationToken = default);
	Task UpdateRoleAsync(long userId, UpdateUserRoleDto dto, long actorUserId, CancellationToken cancellationToken = default);
	Task DeactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
	Task ReactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
	private readonly IUserRepository _userRepository;
	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IAuditLogRepository _auditLogRepository;

	public UserService(
		IUserRepository userRepository,
		IResourceProfileRepository resourceProfileRepository,
		IAuditLogRepository auditLogRepository)
	{
		_userRepository = userRepository;
		_resourceProfileRepository = resourceProfileRepository;
		_auditLogRepository = auditLogRepository;
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

		var now = DateTime.UtcNow;
		var user = await _userRepository.AddAsync(new User
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
		}, cancellationToken);

		var resourceProfile = await _resourceProfileRepository.AddAsync(new ResourceProfile
		{
			UserId = user.Id,
			ResourceProfileCode = $"RES-{user.Id:D6}",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		}, cancellationToken);

		await _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = actorUserId,
			EntityName = "USERS",
			EntityId = user.Id,
			ActionType = "CREATE",
			NewValues = $"{{\"username\":\"{user.Username}\",\"role\":\"{user.Role}\"}}",
			CreatedAt = now
		}, cancellationToken);

		return new UserCreatedDto
		{
			UserId = user.Id,
			EmployeeId = resourceProfile.Id,
			EmployeeCode = resourceProfile.ResourceProfileCode
		};
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

	public Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
	{
		IReadOnlyList<RolePermissionDto> rolePermissions =
		[
			new()
			{
				Role = Roles.Admin,
				Permissions =
				[
					"Create, view, deactivate, reactivate, reset password, and change roles for users",
					"View role permission matrix",
					"Manage employee profiles, skills, managers, freeze restoration, and deactivation",
					"Create and update projects and milestones",
					"View company-wide allocations",
					"Update system configuration and view notification logs"
				]
			},
			new()
			{
				Role = Roles.Manager,
				Permissions =
				[
					"View assigned team resource dashboard and employee drill-down",
					"Allocate team resources to owned ACTIVE or PLANNED projects",
					"End allocations on owned projects",
					"View owned projects, project health, milestones, and team timesheets",
					"Use AI Skill Match, AI Risk Summary, and Team Builder",
					"Restore timesheet access for assigned team members"
				]
			},
			new()
			{
				Role = Roles.Employee,
				Permissions =
				[
					"View own active and historical allocations",
					"Submit own weekly timesheets for allocated projects",
					"View own submitted and missed timesheets",
					"View missed-timesheet reminders"
				]
			}
		];

		return Task.FromResult(rolePermissions);
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

		await _userRepository.SaveChangesAsync(cancellationToken);
		await _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = actorUserId,
			EntityName = "USERS",
			EntityId = user.Id,
			ActionType = "UPDATE_ROLE",
			OldValues = $"{{\"role\":\"{oldRole}\"}}",
			NewValues = $"{{\"role\":\"{user.Role}\"}}",
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);
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
