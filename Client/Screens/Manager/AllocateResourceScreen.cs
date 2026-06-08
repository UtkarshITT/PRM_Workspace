using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class AllocateResourceScreen
{
	private readonly ManagerClient _managerClient;

	public AllocateResourceScreen(ManagerClient managerClient)
	{
		_managerClient = managerClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Allocate Resource");
			Console.WriteLine("  1. Allocate directly (I already know who I want)");
			Console.WriteLine("  2. End an existing allocation");
			Console.WriteLine("  3. Find resource using AI (Phase 8)");
			Console.WriteLine("  0. Back");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await DirectAllocateAsync();
					break;
				case "2":
					await EndAllocationAsync();
					break;
				case "0":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Feature coming in a later phase.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task DirectAllocateAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Direct Allocation");

		if (!long.TryParse(ConsoleHelper.ReadInput("Project ID     : "), out var projectId))
		{
			ConsoleHelper.WriteError("Invalid project ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID    : "), out var employeeId))
		{
			ConsoleHelper.WriteError("Invalid employee ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!decimal.TryParse(ConsoleHelper.ReadInput("Utilisation %  : "), out var percentage) || percentage is < 1 or > 100)
		{
			ConsoleHelper.WriteError("Percentage must be between 1 and 100.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!TryReadDate("From Date (DD-MM-YYYY): ", out var startDate))
		{
			return;
		}

		if (!TryReadDate("To Date (DD-MM-YYYY)  : ", out var endDate))
		{
			return;
		}

		var confirm = ConsoleHelper.ReadInput("Confirm allocation (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var response = await _managerClient.CreateAllocationAsync(new CreateAllocationRequest
		{
			EmployeeId = employeeId,
			ProjectId = projectId,
			AllocationPercentage = percentage,
			AllocationStartDate = DateFormatHelper.ToApiDate(startDate),
			AllocationEndDate = DateFormatHelper.ToApiDate(endDate)
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Allocation created.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task EndAllocationAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("End Allocation");

		if (!long.TryParse(ConsoleHelper.ReadInput("Allocation ID: "), out var allocationId))
		{
			ConsoleHelper.WriteError("Invalid allocation ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var confirm = ConsoleHelper.ReadInput("Confirm end allocation (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var response = await _managerClient.EndAllocationAsync(allocationId);
		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Allocation ended.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static bool TryReadDate(string prompt, out DateOnly date)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		if (DateFormatHelper.TryParseInput(input, out date))
		{
			return true;
		}

		ConsoleHelper.WriteError("Invalid date. Use DD-MM-YYYY.");
		ConsoleHelper.PressEnterToContinue();
		date = default;
		return false;
	}

	private static void WriteApiError<T>(Models.ApiResponse<T> response)
	{
		ConsoleHelper.WriteError(response.Error ?? "Request failed.");

		foreach (var detail in response.Details)
		{
			ConsoleHelper.WriteError($"  - {detail}");
		}
	}
}
