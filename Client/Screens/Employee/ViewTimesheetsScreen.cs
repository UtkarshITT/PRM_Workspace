using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Employee;

public class ViewTimesheetsScreen
{
	private readonly EmployeeClient _employeeClient;

	public ViewTimesheetsScreen(EmployeeClient employeeClient)
	{
		_employeeClient = employeeClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("My Timesheets");

			var response = await _employeeClient.GetMyTimesheetsAsync();
			if (!response.Success || response.Data == null)
			{
				ConsoleHelper.WriteError(response.Error ?? "Failed to load timesheets.");
				ConsoleHelper.PressEnterToContinue();
				return;
			}

			var timesheets = response.Data;
			if (timesheets.Count == 0)
			{
				Console.WriteLine("No timesheets submitted yet.");
			}
			else
			{
				Console.WriteLine($"{"#",3}  {"ID",6}  {"Week Start",-12} {"Total Hrs",9}  Status");
				Console.WriteLine(new string('─', 50));
				for (var index = 0; index < timesheets.Count; index++)
				{
					var timesheet = timesheets[index];
					var weekDate = DateFormatHelper.ParseApiDate(timesheet.WeekStartDate);
					var statusSuffix = timesheet.Status == "MISSED" ? "  ⚠" : string.Empty;
					Console.WriteLine(
						$"{index + 1,2}.  {timesheet.Id,6}  {DateFormatHelper.FormatDisplay(weekDate),-12} {timesheet.TotalHours,5:0} hrs  {timesheet.Status}{statusSuffix}");
				}
			}

			Console.WriteLine();
			Console.WriteLine("[V] View week details     [B] Back");
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim().ToUpperInvariant())
			{
				case "V":
					await ViewDetailAsync(timesheets);
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

	private async Task ViewDetailAsync(IReadOnlyList<Models.Employee.TimesheetListItem> timesheets)
	{
		if (timesheets.Count == 0)
		{
			return;
		}

		if (!int.TryParse(ConsoleHelper.ReadInput($"Select timesheet number (1-{timesheets.Count}): "), out var selection)
		    || selection < 1
		    || selection > timesheets.Count)
		{
			ConsoleHelper.WriteError("Invalid selection.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var timesheetId = timesheets[selection - 1].Id;

		var response = await _employeeClient.GetMyTimesheetDetailAsync(timesheetId);
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
