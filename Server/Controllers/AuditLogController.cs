using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Audit;
using PRM.Server.Models.DTOs.Common;
using PRM.Server.Services.Interfaces;

namespace PRM.Server.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ApiController]
[Route("api/audit-logs")]
public class AuditLogController : ControllerBase
{
	private readonly IAuditService _auditService;

	public AuditLogController(IAuditService auditService)
	{
		_auditService = auditService;
	}

	[HttpGet]
	public async Task<ActionResult<ApiResponse<AuditLogPageDto>>> GetLogs(
		[FromQuery] AuditLogFilterDto filter,
		CancellationToken cancellationToken)
	{
		var logs = await _auditService.GetLogsAsync(filter, cancellationToken);
		return Ok(ApiResponse<AuditLogPageDto>.Ok(logs, "Audit logs retrieved."));
	}
}
