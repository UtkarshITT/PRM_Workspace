using PRM.Server.Models.DTOs.Audit;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;

namespace PRM.Server.Services.Interfaces;

public interface IAuditService
{
	Task LogAsync(
		long actorUserId,
		string action,
		string entityName,
		long entityId,
		string? notes = null,
		string? oldValues = null,
		string? newValues = null,
		CancellationToken cancellationToken = default);

	Task<AuditLogPageDto> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
	private readonly IAuditLogRepository _auditLogRepository;

	public AuditService(IAuditLogRepository auditLogRepository)
	{
		_auditLogRepository = auditLogRepository;
	}

	public Task LogAsync(
		long actorUserId,
		string action,
		string entityName,
		long entityId,
		string? notes = null,
		string? oldValues = null,
		string? newValues = null,
		CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		return _auditLogRepository.WriteAsync(new AuditLog
		{
			ActorUserId = actorUserId,
			ActionType = action,
			EntityName = entityName,
			EntityId = entityId,
			OldValues = oldValues,
			NewValues = BuildNewValues(notes, newValues),
			CreatedAt = now
		}, cancellationToken);
	}

	public async Task<AuditLogPageDto> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default)
	{
		var page = Math.Max(1, filter.Page);
		var pageSize = Math.Clamp(filter.PageSize, 1, 100);
		var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
			page,
			pageSize,
			filter.ActorUserId,
			Normalize(filter.ActionType),
			Normalize(filter.EntityName),
			filter.EntityId,
			filter.From,
			filter.To,
			cancellationToken);

		return new AuditLogPageDto
		{
			Items = items.Select(MapItem).ToList(),
			Page = page,
			PageSize = pageSize,
			TotalCount = totalCount,
			TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
		};
	}

	private static AuditLogItemDto MapItem(AuditLog auditLog)
	{
		return new AuditLogItemDto
		{
			Id = auditLog.Id,
			ActorUserId = auditLog.ActorUserId,
			ActorUsername = auditLog.ActorUser.Username,
			ActorName = auditLog.ActorUser.FullName,
			ActionType = auditLog.ActionType,
			EntityName = auditLog.EntityName,
			EntityId = auditLog.EntityId,
			Notes = ExtractNotes(auditLog.NewValues),
			OldValues = auditLog.OldValues,
			NewValues = auditLog.NewValues,
			CorrelationId = auditLog.CorrelationId,
			CreatedAt = auditLog.CreatedAt
		};
	}

	private static string? Normalize(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static string? BuildNewValues(string? notes, string? newValues)
	{
		if (string.IsNullOrWhiteSpace(notes))
		{
			return newValues;
		}

		if (string.IsNullOrWhiteSpace(newValues))
		{
			return $"{{\"notes\":\"{EscapeJson(notes)}\"}}";
		}

		return newValues;
	}

	private static string? ExtractNotes(string? newValues)
	{
		const string prefix = "{\"notes\":\"";
		const string suffix = "\"}";
		if (newValues == null || !newValues.StartsWith(prefix, StringComparison.Ordinal) || !newValues.EndsWith(suffix, StringComparison.Ordinal))
		{
			return null;
		}

		return newValues.Substring(prefix.Length, newValues.Length - prefix.Length - suffix.Length)
			.Replace("\\\"", "\"", StringComparison.Ordinal)
			.Replace("\\\\", "\\", StringComparison.Ordinal);
	}

	private static string EscapeJson(string value)
	{
		return value.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal);
	}
}

public class NoOpAuditService : IAuditService
{
	public Task LogAsync(
		long actorUserId,
		string action,
		string entityName,
		long entityId,
		string? notes = null,
		string? oldValues = null,
		string? newValues = null,
		CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public Task<AuditLogPageDto> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new AuditLogPageDto());
	}
}
