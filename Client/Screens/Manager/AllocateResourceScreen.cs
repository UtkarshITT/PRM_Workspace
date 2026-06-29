using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class AllocateResourceScreen
{
	private readonly ManagerClient _managerClient;
	private readonly AiClient _aiClient;

	public AllocateResourceScreen(ManagerClient managerClient, AiClient aiClient)
	{
		_managerClient = managerClient;
		_aiClient = aiClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Allocate Resource");
			Console.WriteLine("  1. Find resource using AI (recommended)");
			Console.WriteLine("  2. Allocate directly (I already know who I want)");
			Console.WriteLine("  3. End an existing allocation");
			Console.WriteLine("  4. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await AiSkillMatchAsync();
					break;
				case "2":
					await DirectAllocateAsync();
					break;
				case "3":
					await EndAllocationAsync();
					break;
				case "4":
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

		if (!TryReadPositiveLong("Project ID     : ", "Project ID must be a positive number.", out var projectId))
		{
			return;
		}

		if (!TryReadPositiveLong("Employee ID    : ", "Employee ID must be a positive number.", out var employeeId))
		{
			return;
		}

		if (!decimal.TryParse(ConsoleHelper.ReadInput("Utilisation %  : "), out var percentage) || percentage is < 1 or > 100)
		{
			ConsoleHelper.WriteError("Percentage must be between 1 and 100.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!TryReadDate("From Date (DD-MM-YYYY): ", allowPast: false, out var startDate))
		{
			return;
		}

		if (!TryReadDate("To Date (DD-MM-YYYY)  : ", allowPast: false, out var endDate))
		{
			return;
		}

		if (endDate <= startDate)
		{
			ConsoleHelper.WriteError("To date must be after from date.");
			ConsoleHelper.PressEnterToContinue();
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

	private async Task AiSkillMatchAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("AI Skill Match");
		Console.WriteLine("Describe your project requirement in plain English:");
		Console.Write("> ");
		var requirement = Console.ReadLine()?.Trim();

		if (string.IsNullOrWhiteSpace(requirement))
		{
			ConsoleHelper.WriteError("Requirement cannot be empty.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		Console.WriteLine();
		Console.WriteLine("Searching... (calling AI)");
		var response = await _aiClient.GetSkillMatchAsync(requirement);

		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var data = response.Data;
		if (!string.IsNullOrWhiteSpace(data.Message))
		{
			Console.WriteLine(data.Message);
		}

		if (!string.IsNullOrWhiteSpace(data.Summary))
		{
			Console.WriteLine(data.Summary);
		}

		WriteGapAnalysis(data.GapAnalysis);

		if (data.Candidates.Count == 0)
		{
			Console.WriteLine("No matches found.");
		}
		else
		{
			Console.WriteLine("Results:");
			foreach (var candidate in data.Candidates.OrderBy(item => item.Rank))
			{
				Console.WriteLine($"  {candidate.Rank}. {candidate.FullName} (ID {candidate.EmployeeId})");
				Console.WriteLine($"     {candidate.Reason}");
				Console.WriteLine($"     Availability: {candidate.AvailabilityPercent:0}%");
				Console.WriteLine("     You can allocate this employee, but allocation must be created manually.");
			}
		}

		if (data.AiGenerated && !string.IsNullOrWhiteSpace(data.Disclaimer))
		{
			Console.WriteLine();
			Console.WriteLine($"Note: {data.Disclaimer}");
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

	private static bool TryReadDate(string prompt, bool allowPast, out DateOnly date)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		if (DateFormatHelper.TryParseInput(input, out date))
		{
			if (!allowPast && date < DateOnly.FromDateTime(DateTime.Today))
			{
				ConsoleHelper.WriteError("Date cannot be in the past.");
				ConsoleHelper.PressEnterToContinue();
				return false;
			}

			return true;
		}

		ConsoleHelper.WriteError("Invalid date. Use DD-MM-YYYY.");
		ConsoleHelper.PressEnterToContinue();
		date = default;
		return false;
	}

	private static bool TryReadPositiveLong(string prompt, string error, out long value)
	{
		if (long.TryParse(ConsoleHelper.ReadInput(prompt), out value) && value > 0)
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
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

	private static void WriteGapAnalysis(IReadOnlyList<string> gapAnalysis)
	{
		if (gapAnalysis.Count == 0)
		{
			return;
		}

		Console.WriteLine("Gap analysis:");
		foreach (var gap in gapAnalysis)
		{
			Console.WriteLine($"  - {gap}");
		}

		Console.WriteLine();
	}
}
