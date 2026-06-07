namespace PRM.Server.Models.Entities;

public class ProjectAllocation
{
	public long Id { get; set; }
	public long EmployeeId { get; set; }
	public long ProjectId { get; set; }
	public decimal AllocationPercentage { get; set; }
	public DateOnly AllocationStartDate { get; set; }
	public DateOnly AllocationEndDate { get; set; }
	public string AllocationStatus { get; set; } = "ACTIVE";
	public long AllocatedByManagerId { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public Employee Employee { get; set; } = null!;
	public Project Project { get; set; } = null!;
	public User AllocatedByManager { get; set; } = null!;
}
