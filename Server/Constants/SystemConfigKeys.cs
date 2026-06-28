namespace PRM.Server.Constants;

/// <summary>
/// Predefined SYSTEM_CONFIGURATIONS keys (BRD Screen 3.5).
/// Rows are seeded by migration <c>SeedReferenceData</c>; Admin updates values via Phase 9 API.
/// </summary>
public static class SystemConfigKeys
{
	public const string LlmProvider = "llm_provider";
	public const string LlmApiKey = "llm_api_key";
	public const string SchedulerIntervalHours = "scheduler_interval_hours";
	public const string MaxWeeklyHours = "max_weekly_hours";

	// Legacy rows can exist in older databases, but email delivery is now server-controlled.
	public const string EmailConsoleEnabled = "email_console_enabled";
	public const string EmailSmtpEnabled = "email_smtp_enabled";

	public static readonly IReadOnlySet<string> All =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			LlmProvider,
			LlmApiKey,
			SchedulerIntervalHours,
			MaxWeeklyHours
		};
}
