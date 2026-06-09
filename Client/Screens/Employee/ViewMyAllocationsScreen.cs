using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Employee;

public class ViewMyAllocationsScreen
{
	private readonly EmployeeClient _employeeClient;

	public ViewMyAllocationsScreen(EmployeeClient employeeClient)
	{
		_employeeClient = employeeClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("My Allocations");

		var response = await _employeeClient.GetMyAllocationsAsync();
		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Failed to load allocations.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var allocations = response.Data.Allocations;
		if (allocations.Count == 0)
		{
			Console.WriteLine("No allocations found.");
		}
		else
		{
			Console.WriteLine($"{"Project",-16} {"%",4}  {"From",-11} {"To",-11} Status");
			Console.WriteLine(new string('─', 58));
			foreach (var allocation in allocations)
			{
				var start = DateFormatHelper.ParseApiDate(allocation.AllocationStartDate);
				var end = DateFormatHelper.ParseApiDate(allocation.AllocationEndDate);
				Console.WriteLine(
					$"{allocation.ProjectName,-16} {allocation.AllocationPercentage,3:0}%  " +
					$"{DateFormatHelper.FormatDisplay(start),-11} {DateFormatHelper.FormatDisplay(end),-11} {allocation.AllocationStatus}");
			}

			Console.WriteLine(new string('─', 58));
			Console.WriteLine($"Total Utilisation: {response.Data.TotalActiveUtilizationPercent:0}%");
		}

		ConsoleHelper.PressEnterToContinue();
	}
}
