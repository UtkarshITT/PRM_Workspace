namespace PRM.Server.Models.Entities;

public class Skill
{
	public long Id { get; set; }
	public string SkillName { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; }

	public ICollection<ResourceProfileSkill> ResourceProfileSkills { get; set; } = [];
}
