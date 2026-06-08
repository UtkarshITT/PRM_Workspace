namespace PRM.Server.Constants;

public static class MilestoneStatuses
{
	public const string NotStarted = "NOT_STARTED";
	public const string InProgress = "IN_PROGRESS";
	public const string Done = "DONE";

	public static readonly string[] All = [NotStarted, InProgress, Done];
}
