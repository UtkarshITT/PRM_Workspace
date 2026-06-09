using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class TeamTimesheetsScreen
{
	private readonly ManagerClient _managerClient;

	public TeamTimesheetsScreen(ManagerClient managerClient)
	{
		_managerClient = managerClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Timesheets — My Team");

			var today = DateOnly.FromDateTime(DateTime.Today);
			var defaultWeek = DateFormatHelper.GetWeekStart(today);
			var weekInput = ConsoleHelper.ReadInput(
				$"Filter by week (DD-MM-YYYY) or Enter for {DateFormatHelper.FormatDisplay(defaultWeek)}: ");

			DateOnly weekStart;
			if (string.IsNullOrWhiteSpace(weekInput))
			{
				weekStart = defaultWeek;
			}
			else if (!DateFormatHelper.TryParseInput(weekInput, out weekStart))
			{
				ConsoleHelper.WriteError("Invalid date format.");
				ConsoleHelper.PressEnterToContinue();
				continue;
			}
			else
			{
				weekStart = DateFormatHelper.GetWeekStart(weekStart);
			}

			Console.WriteLine();
			Console.WriteLine($"Week: {DateFormatHelper.FormatDisplay(weekStart)}");
			Console.WriteLine(new string('─', 46));

			var response = await _managerClient.GetTeamTimesheetsAsync(DateFormatHelper.ToApiDate(weekStart));
			if (!response.Success || response.Data == null)
			{
				ConsoleHelper.WriteError(response.Error ?? "Failed to load team timesheets.");
				ConsoleHelper.PressEnterToContinue();
				continue;
			}

			var rows = response.Data;
			if (rows.Count == 0)
			{
				Console.WriteLine("No timesheet entries for this week.");
			}
			else
			{
				Console.WriteLine($"{"#",3}  {"ID",6}  {"Employee",-14} {"Project",-12} {"Hrs",5}  Status");
				Console.WriteLine(new string('─', 54));
				for (var index = 0; index < rows.Count; index++)
				{
					var row = rows[index];
					var statusSuffix = row.Status == "MISSED" ? " ⚠" : string.Empty;
					Console.WriteLine(
						$"{index + 1,2}.  {row.TimesheetId,6}  {row.EmployeeName,-14} {row.ProjectName,-12} {row.HoursLogged,4:0}   {row.Status}{statusSuffix}");
				}
			}

			Console.WriteLine();
			Console.WriteLine("[V] View employee timesheet detail     [B] Back");
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim().ToUpperInvariant())
			{
				case "V":
					await ViewDetailAsync(rows);
					break;
				case "B":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task ViewDetailAsync(IReadOnlyList<TeamTimesheetRow> rows)
	{
		if (rows.Count == 0)
		{
			return;
		}

		if (!int.TryParse(ConsoleHelper.ReadInput($"Select row number (1-{rows.Count}): "), out var selection)
		    || selection < 1
		    || selection > rows.Count)
		{
			ConsoleHelper.WriteError("Invalid selection.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var timesheetId = rows[selection - 1].TimesheetId;

		var response = await _managerClient.GetTimesheetDetailAsync(timesheetId);
		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Failed to load timesheet detail.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var detail = response.Data;
		var weekDate = DateFormatHelper.ParseApiDate(detail.WeekStartDate);

		Console.Clear();
		ConsoleHelper.WriteHeader($"Week: {DateFormatHelper.FormatDisplay(weekDate)} — Status: {detail.Status}");
		Console.WriteLine();
		Console.WriteLine($"{"Project",-16} {"Hrs",5}  Activity Tags");
		Console.WriteLine(new string('─', 46));

		foreach (var lineItem in detail.LineItems)
		{
			Console.WriteLine(
				$"{lineItem.ProjectName,-16} {lineItem.HoursLogged,4:0}   {string.Join(", ", lineItem.ActivityTags)}");
		}

		Console.WriteLine(new string('─', 46));
		Console.WriteLine($"Total: {detail.TotalHours:0} hrs");
		if (!string.IsNullOrWhiteSpace(detail.Remarks))
		{
			Console.WriteLine($"Remarks: {detail.Remarks}");
		}

		ConsoleHelper.PressEnterToContinue();
	}
}
