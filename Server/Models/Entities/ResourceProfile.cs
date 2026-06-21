namespace PRM.Server.Models.Entities;

public class ResourceProfile
{
	public long Id { get; set; }
	public long UserId { get; set; }
	public long? ManagerId { get; set; }
	public string ResourceProfileCode { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
	public string EmploymentStatus { get; set; } = "BENCH";
	public bool IsActive { get; set; } = true;
	public bool IsTimesheetFrozen { get; set; }
	public DateTime? TimesheetFrozenAt { get; set; }
	public DateOnly? JoinedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public User User { get; set; } = null!;
	public User? Manager { get; set; }
	public ICollection<ResourceProfileSkill> ResourceProfileSkills { get; set; } = [];
	public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = [];
	public ICollection<Timesheet> Timesheets { get; set; } = [];
	public ICollection<TimesheetComplianceTracking> ComplianceTracking { get; set; } = [];
}
