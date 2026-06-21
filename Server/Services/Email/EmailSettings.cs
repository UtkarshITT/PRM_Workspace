namespace PRM.Server.Services.Email;

public class EmailSettings
{
	public bool ConsoleEnabled { get; init; } = true;
	public bool SmtpEnabled { get; init; } = true;
	public string SmtpHost { get; init; } = string.Empty;
	public int SmtpPort { get; init; } = 587;
	public string SmtpUsername { get; init; } = string.Empty;
	public string SmtpPassword { get; init; } = string.Empty;
	public string SmtpFromAddress { get; init; } = string.Empty;

	public bool IsSmtpConfigured =>
		!string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(SmtpFromAddress);
}
