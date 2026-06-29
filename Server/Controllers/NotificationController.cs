using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Models.DTOs.Notifications;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
	private readonly INotificationLogService _notificationLogService;

	public NotificationController(INotificationLogService notificationLogService)
	{
		_notificationLogService = notificationLogService;
	}

	[HttpGet("logs")]
	public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationLogDto>>>> GetLogs(
		[FromQuery] int take,
		CancellationToken cancellationToken)
	{
		var logs = await _notificationLogService.GetRecentAsync(take <= 0 ? 50 : take, cancellationToken);
		return Ok(ApiResponse<IReadOnlyList<NotificationLogDto>>.Ok(logs, "Notification logs retrieved."));
	}
}
