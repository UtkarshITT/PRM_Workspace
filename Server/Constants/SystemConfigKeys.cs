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

	public static readonly IReadOnlySet<string> All =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			LlmProvider,
			LlmApiKey,
			SchedulerIntervalHours,
			MaxWeeklyHours
		};
}
