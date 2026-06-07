namespace PRM.Server.Models.Entities;

public class ActivityTag
{
	public long Id { get; set; }
	public string TagCode { get; set; } = string.Empty;
	public string TagName { get; set; } = string.Empty;
	public string? TagCategory { get; set; }
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; }

	public ICollection<TimesheetLineItemActivityTag> LineItemTags { get; set; } = [];
}
