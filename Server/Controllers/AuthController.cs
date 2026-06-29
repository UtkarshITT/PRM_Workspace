using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Extensions;
using PRM.Server.Models.DTOs.Auth;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;

	public AuthController(IAuthService authService)
	{
		_authService = authService;
	}

	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
		[FromBody] LoginDto dto,
		CancellationToken cancellationToken)
	{
		var result = await _authService.LoginAsync(dto, cancellationToken);
		return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
	}

	[Authorize]
	[HttpPost("change-password")]
	public async Task<ActionResult<ApiResponse<LoginResponseDto>>> ChangePassword(
		[FromBody] PasswordChangeDto dto,
		CancellationToken cancellationToken)
	{
		var userId = User.GetUserId();
		var result = await _authService.ChangePasswordAsync(userId, dto, cancellationToken);
		return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Password changed successfully."));
	}
}
