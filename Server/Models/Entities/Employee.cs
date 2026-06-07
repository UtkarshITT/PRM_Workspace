namespace PRM.Server.Models.Entities;

public class Employee
{
	public long Id { get; set; }
	public long UserId { get; set; }
	public long? ManagerId { get; set; }
	public string EmployeeCode { get; set; } = string.Empty;
	public string? Department { get; set; }
	public string? Designation { get; set; }
	public string EmploymentStatus { get; set; } = "BENCH";
	public bool IsActive { get; set; } = true;
	public DateOnly? JoinedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public User User { get; set; } = null!;
	public User? Manager { get; set; }
	public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = [];
	public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = [];
	public ICollection<Timesheet> Timesheets { get; set; } = [];
}
