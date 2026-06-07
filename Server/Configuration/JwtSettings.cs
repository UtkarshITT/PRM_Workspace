namespace PRM.Server.Configuration;

public class JwtSettings
{
	public const string SectionName = "JwtSettings";

	public string SecretKey { get; set; } = string.Empty;
	public string Issuer { get; set; } = "PRM.Server";
	public string Audience { get; set; } = "PRM.Client";
	public int ExpiryHours { get; set; } = 8;
}
