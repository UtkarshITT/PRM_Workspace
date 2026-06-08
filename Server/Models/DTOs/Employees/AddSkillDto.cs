namespace PRM.Server.Models.DTOs.Employees;

public class AddSkillDto
{
	public string SkillName { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string ProficiencyLevel { get; set; } = string.Empty;
}
