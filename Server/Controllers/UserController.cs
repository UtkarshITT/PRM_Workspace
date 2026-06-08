using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly IValidator<CreateUserDto> _createUserValidator;
	private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;

	public UserController(
		IUserService userService,
		IValidator<CreateUserDto> createUserValidator,
		IValidator<ResetPasswordDto> resetPasswordValidator)
	{
		_userService = userService;
		_createUserValidator = createUserValidator;
		_resetPasswordValidator = resetPasswordValidator;
	}

	[HttpPost]
	public async Task<ActionResult<ApiResponse<UserCreatedDto>>> CreateUser(
		[FromBody] CreateUserDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_createUserValidator, dto, cancellationToken);

		var result = await _userService.CreateUserAccountAsync(dto, GetUserId(), cancellationToken);
		return StatusCode(StatusCodes.Status201Created,
			ApiResponse<UserCreatedDto>.Ok(result, "Account created. User must change password on first login."));
	}

	[HttpGet]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListItemDto>>>> GetAllUsers(
		CancellationToken cancellationToken)
	{
		var users = await _userService.GetAllUsersAsync(cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<UserListItemDto>>.Ok(users, "Users retrieved."));
	}

	[HttpPut("{id:long}/reset-password")]
	public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
		long id,
		[FromBody] ResetPasswordDto dto,
		CancellationToken cancellationToken)
	{
		await ValidationHelper.ValidateAsync(_resetPasswordValidator, dto, cancellationToken);
		await _userService.ResetPasswordAsync(id, dto, cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "Password reset. User must change it on next login."));
	}

	[HttpPut("{id:long}/deactivate")]
	public async Task<ActionResult<ApiResponse<object>>> DeactivateUser(long id, CancellationToken cancellationToken)
	{
		await _userService.DeactivateUserAsync(id, GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "User account deactivated."));
	}

	[HttpPut("{id:long}/reactivate")]
	public async Task<ActionResult<ApiResponse<object>>> ReactivateUser(long id, CancellationToken cancellationToken)
	{
		await _userService.ReactivateUserAsync(id, GetUserId(), cancellationToken);
		return Ok(ApiResponse<object>.Ok(new { }, "User account reactivated."));
	}

	private long GetUserId()
	{
		var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return long.Parse(userIdClaim!);
	}
}
