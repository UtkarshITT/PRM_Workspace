using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Employee;

public class EmployeeMenuScreen
{
	private readonly EmployeeClient _employeeClient;
	private readonly SubmitTimesheetScreen _submitTimesheetScreen;
	private readonly ViewTimesheetsScreen _viewTimesheetsScreen;
	private readonly ViewMyAllocationsScreen _viewMyAllocationsScreen;

	public EmployeeMenuScreen(
		EmployeeClient employeeClient,
		SubmitTimesheetScreen submitTimesheetScreen,
		ViewTimesheetsScreen viewTimesheetsScreen,
		ViewMyAllocationsScreen viewMyAllocationsScreen)
	{
		_employeeClient = employeeClient;
		_submitTimesheetScreen = submitTimesheetScreen;
		_viewTimesheetsScreen = viewTimesheetsScreen;
		_viewMyAllocationsScreen = viewMyAllocationsScreen;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Employee Menu — Welcome, {SessionStore.FullName}");
			await ShowRemindersAsync();
			Console.WriteLine("  1. Submit Timesheet");
			Console.WriteLine("  2. View My Timesheets");
			Console.WriteLine("  3. View My Allocations");
			Console.WriteLine("  0. Logout");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await _submitTimesheetScreen.ShowAsync();
					break;
				case "2":
					await _viewTimesheetsScreen.ShowAsync();
					break;
				case "3":
					await _viewMyAllocationsScreen.ShowAsync();
					break;
				case "0":
					SessionStore.Clear();
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task ShowRemindersAsync()
	{
		var response = await _employeeClient.GetRemindersAsync();
		if (!response.Success || response.Data?.Messages.Count == 0)
		{
			return;
		}

		foreach (var message in response.Data!.Messages)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"  ⚠  {message}");
			Console.ResetColor();
		}

		Console.WriteLine();
	}
}
