using Microsoft.EntityFrameworkCore;
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
	Task DeactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
	Task ReactivateUserAsync(long userId, long actorUserId, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
	private readonly PrmDbContext _context;
	private readonly IUserRepository _userRepository;
	private readonly IEmployeeRepository _employeeRepository;

	public UserService(
		PrmDbContext context,
		IUserRepository userRepository,
		IEmployeeRepository employeeRepository)
	{
		_context = context;
		_userRepository = userRepository;
		_employeeRepository = employeeRepository;
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

			var employee = new Employee
			{
				UserId = user.Id,
				EmployeeCode = $"EMP-{user.Id:D6}",
				EmploymentStatus = "BENCH",
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			};

			_context.Employees.Add(employee);

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
				EmployeeId = employee.Id,
				EmployeeCode = employee.EmployeeCode
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

		var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
		if (employee != null)
		{
			employee.IsActive = false;
			employee.UpdatedAt = DateTime.UtcNow;
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

		var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
		if (employee != null)
		{
			employee.IsActive = true;
			employee.UpdatedAt = DateTime.UtcNow;
		}

		await _userRepository.SaveChangesAsync(cancellationToken);
	}
}
