namespace PRM.Server.Models.Entities;

public class SystemConfiguration
{
	public long Id { get; set; }
	public string ConfigKey { get; set; } = string.Empty;
	public string ConfigValue { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTime UpdatedAt { get; set; }
	public long? UpdatedByUserId { get; set; }

	public User? UpdatedByUser { get; set; }
}
