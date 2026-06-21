namespace PRM.Server.Models.Entities;

public class ResourceProfileSkill
{
	public long ResourceProfileId { get; set; }
	public long SkillId { get; set; }
	public string ProficiencyLevel { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }

	public ResourceProfile ResourceProfile { get; set; } = null!;
	public Skill Skill { get; set; } = null!;
}
