namespace PRM.Client.Helpers;

public static class HealthStatusHelper
{
	public static string ToDisplayLabel(string healthStatus) => healthStatus.ToUpperInvariant() switch
	{
		"GREEN" => "ON TRACK",
		"AMBER" => "ATTENTION",
		"RED" => "AT RISK",
		_ => healthStatus
	};

	public static string ToDisplayIcon(string healthStatus) => healthStatus.ToUpperInvariant() switch
	{
		"GREEN" => "🟢",
		"AMBER" => "🟡",
		"RED" => "🔴",
		_ => "•"
	};
}
