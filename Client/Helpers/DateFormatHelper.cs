using System.Globalization;

namespace PRM.Client.Helpers;

public static class DateFormatHelper
{
	private static readonly string[] DisplayFormats = ["dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy"];

	public static string FormatDisplay(DateOnly date) =>
		date.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

	public static bool TryParseInput(string input, out DateOnly date)
	{
		return DateOnly.TryParseExact(
			input.Trim(),
			DisplayFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out date);
	}

	public static string ToApiDate(DateOnly date) =>
		date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	public static DateOnly GetWeekStart(DateOnly date)
	{
		var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
		return date.AddDays(-daysFromMonday);
	}

	public static DateOnly GetLastCompletedWeekStart(DateOnly today) =>
		GetWeekStart(today).AddDays(-7);

	public static DateOnly ParseApiDate(string apiDate) =>
		DateOnly.Parse(apiDate, CultureInfo.InvariantCulture);
}
