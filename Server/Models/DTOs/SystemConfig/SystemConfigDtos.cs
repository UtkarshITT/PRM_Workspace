namespace PRM.Server.Models.DTOs.SystemConfig;

public class SystemConfigItemDto
{
	public string Key { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
	public string? Description { get; set; }
	public bool IsSecret { get; set; }
	public bool IsConfigured { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class UpdateSystemConfigDto
{
	public string? LlmProvider { get; set; }
	public string? LlmApiKey { get; set; }
	public int? SchedulerIntervalHours { get; set; }
	public int? MaxWeeklyHours { get; set; }
	public bool? EmailConsoleEnabled { get; set; }
	public bool? EmailSmtpEnabled { get; set; }
}
