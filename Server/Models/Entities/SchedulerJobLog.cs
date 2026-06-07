namespace PRM.Server.Models.Entities;

public class SchedulerJobLog
{
	public long Id { get; set; }
	public string JobName { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTime StartedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public string? ErrorMessage { get; set; }
}
