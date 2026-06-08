using System.Globalization;
using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Admin;

public class ViewAllocationsScreen
{
	private readonly AdminClient _adminClient;

	public ViewAllocationsScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("All Allocations");

		Console.Write("Filter by Employee ID (or Enter): ");
		var employeeInput = Console.ReadLine()?.Trim();
		Console.Write("Filter by Project ID (or Enter): ");
		var projectInput = Console.ReadLine()?.Trim();

		long? employeeId = long.TryParse(employeeInput, out var parsedEmployee) ? parsedEmployee : null;
		long? projectId = long.TryParse(projectInput, out var parsedProject) ? parsedProject : null;

		var response = await _adminClient.GetAllocationsAsync(employeeId, projectId);
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		Console.WriteLine($"{"Employee",-18}{"Project",-18}{"%",-6}{"From",-12}{"To",-12}Status");
		Console.WriteLine(new string('-', 80));

		foreach (var allocation in response.Data)
		{
			var from = DateOnly.Parse(allocation.AllocationStartDate, CultureInfo.InvariantCulture);
			var to = DateOnly.Parse(allocation.AllocationEndDate, CultureInfo.InvariantCulture);
			Console.WriteLine(
				$"{allocation.EmployeeName,-18}{allocation.ProjectName,-18}{allocation.AllocationPercentage,4:0}%  {DateFormatHelper.FormatDisplay(from),-12}{DateFormatHelper.FormatDisplay(to),-12}{allocation.AllocationStatus}");
		}

		Console.WriteLine();
		Console.WriteLine($"Total Active Allocations: {response.Data.Count}");
		ConsoleHelper.PressEnterToContinue();
	}

	private static void WriteApiError<T>(Models.ApiResponse<T> response)
	{
		ConsoleHelper.WriteError(response.Error ?? "Request failed.");

		foreach (var detail in response.Details)
		{
			ConsoleHelper.WriteError($"  - {detail}");
		}

		ConsoleHelper.PressEnterToContinue();
	}
}
