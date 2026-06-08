namespace PRM.Server.Constants;

public static class ProjectStatuses
{
	public const string Planned = "PLANNED";
	public const string Active = "ACTIVE";
	public const string OnHold = "ON_HOLD";
	public const string Completed = "COMPLETED";

	public static readonly string[] All = [Planned, Active, OnHold, Completed];
}
