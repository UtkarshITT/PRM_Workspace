namespace PRM.Server.Helpers;

public static class WeekHelper
{
	public static DateOnly GetWeekStart(DateOnly date)
	{
		var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
		return date.AddDays(-daysFromMonday);
	}

	public static DateOnly GetWeekEnd(DateOnly weekStart) => weekStart.AddDays(6);

	public static DateOnly GetLastCompletedWeekStart(DateOnly today) =>
		GetWeekStart(today).AddDays(-7);

	public static DateOnly GetDefaultSubmitWeekStart(DateOnly today) =>
		GetLastCompletedWeekStart(today);
}
